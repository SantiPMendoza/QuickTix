using QuickTix.Contracts.Common;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace QuickTix.Desktop.Services
{
    /// <summary>
    /// Cliente HTTP genérico para consumir la API QuickTix.
    /// Soporta autenticación JWT y lanza ApiException con códigos HTTP.
    /// </summary>
    public class HttpJsonClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenStore _tokenStore;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public HttpJsonClient(HttpClient httpClient, ITokenStore tokenStore)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        }

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
                    // Nuevo contrato ApiResponse<T>
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

                    // Compatibilidad antigua
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
