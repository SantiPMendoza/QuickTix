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
        private readonly IAuthService _authService;

        public HttpJsonClient(HttpClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        private void AddAuthorizationHeader()
        {
            var token = _authService.GetToken();
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        /// <summary>
        /// Extrae el mensaje de error del cuerpo JSON devuelto por la API.
        /// </summary>
        private async Task<string> ExtractErrorMessage(HttpResponseMessage response)
        {
            try
            {
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json))
                    return response.ReasonPhrase ?? "Error desconocido.";

                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (parsed == null)
                    return json;

                // Compatibilidad con diferentes formatos de API
                if (parsed.TryGetValue("message", out var msg)) return msg;
                if (parsed.TryGetValue("mensaje", out var mensaje)) return mensaje;
                if (parsed.TryGetValue("error", out var err)) return err;

                return json;
            }
            catch
            {
                return response.ReasonPhrase ?? "Error desconocido.";
            }
        }

        // -------------------- MÉTODOS HTTP --------------------

        public async Task<List<T>> GetListAsync<T>(string url)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    throw new ApiException(await ExtractErrorMessage(response), response.StatusCode);

                var result = await response.Content.ReadFromJsonAsync<List<T>>();
                return result ?? throw new ApiException("La respuesta fue nula o vacía.", HttpStatusCode.NoContent);
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

                if (!response.IsSuccessStatusCode)
                    throw new ApiException(await ExtractErrorMessage(response), response.StatusCode);

                return await response.Content.ReadFromJsonAsync<T>();
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
                var response = await _httpClient.PostAsJsonAsync(url, data);

                if (!response.IsSuccessStatusCode)
                    throw new ApiException(await ExtractErrorMessage(response), response.StatusCode);

                var result = await response.Content.ReadFromJsonAsync<TResponse>();
                return result ?? throw new ApiException("La respuesta fue nula tras el POST.", HttpStatusCode.NoContent);
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
                var response = await _httpClient.PutAsJsonAsync(url, data);

                if (!response.IsSuccessStatusCode)
                    throw new ApiException(await ExtractErrorMessage(response), response.StatusCode);
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

                if (!response.IsSuccessStatusCode)
                    throw new ApiException(await ExtractErrorMessage(response), response.StatusCode);
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
