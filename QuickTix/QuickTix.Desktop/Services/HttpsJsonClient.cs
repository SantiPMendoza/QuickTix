using QuickTix.Contracts.Common;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace QuickTix.Desktop.Services
{
    /// <summary>
    /// Cliente HTTP genérico para consumir la API QuickTix desde el cliente Desktop.
    /// Añade automáticamente el token JWT a las peticiones y procesa el contrato ApiResponse;.
    /// Lanza <see cref="ApiException"/> con el código HTTP y un mensaje extraído de la respuesta.
    /// </summary>
    public class HttpJsonClient
    {
        // HttpClient configurado por DI (BaseAddress, handlers, timeouts, etc.)
        private readonly HttpClient _httpClient;

        // Store que contiene el token JWT que debe enviarse como Bearer en cada llamada
        private readonly TokenStore _tokenStore;

        // Opciones JSON comunes para deserialización flexible (case-insensitive)
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="HttpJsonClient"/>.
        /// </summary>
        /// <param name="httpClient">HttpClient inyectado.</param>
        /// <param name="tokenStore">Store del token JWT.</param>
        public HttpJsonClient(HttpClient httpClient, TokenStore tokenStore)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        }

        /// <summary>
        /// Añade o limpia el header Authorization según exista un token válido en el store.
        /// </summary>
        private void AddAuthorizationHeader()
        {
            var token = _tokenStore.GetToken();
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        /// <summary>
        /// Extrae un mensaje de error representativo desde la respuesta HTTP.
        /// Intenta interpretar el contenido como JSON y recuperar campos típicos de error.
        /// </summary>
        /// <param name="response">Respuesta HTTP.</param>
        /// <returns>Mensaje de error interpretado.</returns>
        private async Task<string> ExtractErrorMessage(HttpResponseMessage response)
        {
            try
            {
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json))
                    return response.ReasonPhrase ?? "Error desconocido.";

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    // Contrato ApiResponse<T>: lista de mensajes de error
                    if (root.TryGetProperty("errorMessages", out var errors) && errors.ValueKind == JsonValueKind.Array)
                    {
                        var list = errors.EnumerateArray()
                            .Where(e => e.ValueKind == JsonValueKind.String)
                            .Select(e => e.GetString())
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList();

                        if (list.Count > 0)
                            return string.Join(" ", list);
                    }

                    // Compatibilidad con formatos antiguos o alternativos
                    if (root.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                        return msg.GetString() ?? "Error desconocido.";

                    if (root.TryGetProperty("mensaje", out var mensaje) && mensaje.ValueKind == JsonValueKind.String)
                        return mensaje.GetString() ?? "Error desconocido.";

                    if (root.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.String)
                        return err.GetString() ?? "Error desconocido.";
                }

                // Si no es un objeto JSON reconocible, se devuelve el contenido íntegro
                return json;
            }
            catch
            {
                return response.ReasonPhrase ?? "Error desconocido.";
            }
        }

        /// <summary>
        /// Lee y valida el resultado bajo el contrato <see cref="ApiResponse{T}"/>.
        /// Si el servidor devuelve un JSON plano y el status es OK, intenta un fallback a T.
        /// </summary>
        /// <typeparam name="T">Tipo de resultado esperado.</typeparam>
        /// <param name="response">Respuesta HTTP.</param>
        /// <returns>Resultado deserializado o null si el contrato lo permite.</returns>
        /// <exception cref="ApiException">Cuando la respuesta no es exitosa o el contrato indica error.</exception>
        private async Task<T?> ReadApiResultAsync<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            ApiResponse<T>? api;

            try
            {
                api = JsonSerializer.Deserialize<ApiResponse<T>>(content, JsonOptions);
            }
            catch (JsonException)
            {
                // Fallback temporal si queda algún endpoint devolviendo JSON plano
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        return JsonSerializer.Deserialize<T>(content, JsonOptions);
                    }
                    catch (JsonException)
                    {
                        throw new ApiException("No se pudo interpretar la respuesta del servidor.", HttpStatusCode.InternalServerError);
                    }
                }

                throw new ApiException(await ExtractErrorMessage(response), response.StatusCode);
            }

            if (api == null)
                throw new ApiException("Respuesta del servidor vacía o inválida.", HttpStatusCode.InternalServerError);

            if (!response.IsSuccessStatusCode || !api.IsSuccess)
            {
                var status = api.StatusCode != 0 ? api.StatusCode : response.StatusCode;

                var message = (api.ErrorMessages != null && api.ErrorMessages.Count > 0)
                    ? string.Join(" ", api.ErrorMessages)
                    : await ExtractErrorMessage(response);

                throw new ApiException(message, status);
            }

            return api.Result;
        }

        /// <summary>
        /// Realiza una petición GET que devuelve un listado de elementos.
        /// </summary>
        /// <typeparam name="T">Tipo del elemento.</typeparam>
        /// <param name="url">Ruta relativa o absoluta del endpoint.</param>
        /// <returns>Listado (vacío si la API devuelve null).</returns>
        public async Task<List<T>> GetListAsync<T>(string url)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.GetAsync(url);

                var result = await ReadApiResultAsync<List<T>>(response);
                return result ?? new List<T>();
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error obteniendo lista de {typeof(T).Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Realiza una petición GET que devuelve un elemento.
        /// </summary>
        /// <typeparam name="T">Tipo del resultado.</typeparam>
        /// <param name="url">Ruta relativa o absoluta del endpoint.</param>
        /// <returns>Elemento deserializado o null.</returns>
        public async Task<T?> GetAsync<T>(string url)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.GetAsync(url);

                return await ReadApiResultAsync<T>(response);
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error obteniendo {typeof(T).Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Realiza una petición POST enviando un cuerpo JSON y devuelve un resultado tipado.
        /// </summary>
        /// <typeparam name="TRequest">Tipo del request.</typeparam>
        /// <typeparam name="TResponse">Tipo del response.</typeparam>
        /// <param name="url">Ruta relativa o absoluta del endpoint.</param>
        /// <param name="data">Payload del request.</param>
        /// <returns>Resultado deserializado.</returns>
        public async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest data)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.PostAsJsonAsync(url, data, JsonOptions);

                var result = await ReadApiResultAsync<TResponse>(response);

                if (result == null)
                    throw new ApiException("La respuesta fue nula tras el POST.", HttpStatusCode.InternalServerError);

                return result;
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error en POST {url}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Realiza una petición PUT enviando un cuerpo JSON.
        /// </summary>
        /// <typeparam name="T">Tipo del payload.</typeparam>
        /// <param name="url">Ruta relativa o absoluta del endpoint.</param>
        /// <param name="data">Payload a enviar.</param>
        /// <returns>Tarea asíncrona.</returns>
        public async Task PutAsync<T>(string url, T data)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.PutAsJsonAsync(url, data, JsonOptions);

                await ReadApiResultAsync<object>(response);
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error en PUT {url}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Realiza una petición DELETE.
        /// </summary>
        /// <param name="url">Ruta relativa o absoluta del endpoint.</param>
        /// <returns>Tarea asíncrona.</returns>
        public async Task DeleteAsync(string url)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.DeleteAsync(url);

                await ReadApiResultAsync<object>(response);
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error en DELETE {url}: {ex.Message}", ex);
            }
        }
    }
}
