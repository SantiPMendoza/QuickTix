using QuickTix.Contracts.Common;
using QuickTix.Mobile.Helpers;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace QuickTix.Mobile.Services
{
    /// <summary>
    /// Cliente HTTP genérico para consumir la API de QuickTix desde Mobile.
    /// Adjunta automáticamente el token Bearer (si existe), procesa el contrato <see cref="ApiResponse{T}"/>
    /// y unifica errores mediante <see cref="ApiException"/>.
    /// </summary>
    public class HttpJsonClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenStore _tokenStore;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="HttpJsonClient"/>.
        /// </summary>
        /// <param name="httpClient">HttpClient configurado con la BaseAddress de la API.</param>
        /// <param name="tokenStore">Almacén del token JWT.</param>
        public HttpJsonClient(HttpClient httpClient, ITokenStore tokenStore)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        }

        /// <summary>
        /// Adjunta el header Authorization con esquema Bearer cuando existe token en el store.
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
        /// Intenta extraer un mensaje de error útil desde el body cuando el servidor no devuelve ApiResponse válido.
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

                    if (root.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                        return msg.GetString() ?? "Error desconocido.";

                    if (root.TryGetProperty("mensaje", out var mensaje) && mensaje.ValueKind == JsonValueKind.String)
                        return mensaje.GetString() ?? "Error desconocido.";

                    if (root.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.String)
                        return err.GetString() ?? "Error desconocido.";
                }

                return json;
            }
            catch
            {
                return response.ReasonPhrase ?? "Error desconocido.";
            }
        }

        /// <summary>
        /// Deserializa el cuerpo como <see cref="ApiResponse{T}"/> y devuelve el Result si la operación fue correcta.
        /// En caso de error, lanza <see cref="ApiException"/> con el código HTTP correspondiente.
        /// </summary>
        /// <typeparam name="T">Tipo del Result esperado.</typeparam>
        /// <param name="response">Respuesta HTTP.</param>
        /// <returns>Resultado deserializado o null.</returns>
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
                        throw new ApiException(
                            "No se pudo interpretar la respuesta del servidor.",
                            HttpStatusCode.InternalServerError);
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
        /// Realiza un GET que devuelve un listado.
        /// </summary>
        /// <typeparam name="T">Tipo de elemento.</typeparam>
        /// <param name="url">Ruta relativa del endpoint.</param>
        /// <returns>Listado de elementos (vacío si no hay datos).</returns>
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
        /// Realiza un GET que devuelve un objeto.
        /// </summary>
        /// <typeparam name="T">Tipo de respuesta.</typeparam>
        /// <param name="url">Ruta relativa del endpoint.</param>
        /// <returns>Objeto deserializado o null.</returns>
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
        /// Realiza un POST con body JSON y devuelve el objeto de respuesta.
        /// </summary>
        /// <typeparam name="TRequest">Tipo del body.</typeparam>
        /// <typeparam name="TResponse">Tipo de la respuesta.</typeparam>
        /// <param name="url">Ruta relativa del endpoint.</param>
        /// <param name="data">Payload.</param>
        /// <returns>Respuesta deserializada.</returns>
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
        /// Realiza un PUT con body JSON.
        /// </summary>
        /// <typeparam name="T">Tipo del body.</typeparam>
        /// <param name="url">Ruta relativa del endpoint.</param>
        /// <param name="data">Payload.</param>
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
        /// Realiza un DELETE.
        /// </summary>
        /// <param name="url">Ruta relativa del endpoint.</param>
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
