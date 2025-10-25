using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace QuickTix.Desktop.Services
{
    /// <summary>
    /// Cliente HTTP genérico para consumir la API QuickTix con soporte JWT.
    /// </summary>
    public class HttpJsonClient
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;

        public HttpJsonClient(HttpClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
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

        private async Task<string> ExtractErrorMessage(HttpResponseMessage response)
        {
            try
            {
                var json = await response.Content.ReadAsStringAsync();
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                return parsed != null && parsed.TryGetValue("message", out var msg)
                    ? msg
                    : json;
            }
            catch
            {
                return response.ReasonPhrase ?? "Error desconocido";
            }
        }

        public async Task<List<T>> GetListAsync<T>(string url)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new Exception(await ExtractErrorMessage(response));

            return await response.Content.ReadFromJsonAsync<List<T>>() ?? new();
        }

        public async Task<T?> GetAsync<T>(string url)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new Exception(await ExtractErrorMessage(response));

            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest data)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync(url, data);
            if (!response.IsSuccessStatusCode)
                throw new Exception(await ExtractErrorMessage(response));

            return await response.Content.ReadFromJsonAsync<TResponse>()
                   ?? throw new Exception("La respuesta fue nula tras el POST.");
        }

        public async Task PutAsync(string url)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PutAsync(url, null);
            if (!response.IsSuccessStatusCode)
                throw new Exception(await ExtractErrorMessage(response));
        }

        public async Task PutAsync<T>(string url, T data)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PutAsJsonAsync(url, data);
            if (!response.IsSuccessStatusCode)
                throw new Exception(await ExtractErrorMessage(response));
        }

        public async Task DeleteAsync(string url)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.DeleteAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new Exception(await ExtractErrorMessage(response));
        }
    }
}
