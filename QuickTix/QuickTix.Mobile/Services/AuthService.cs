using QuickTix.Contracts.Common;
using QuickTix.Contracts.DTOs.UserAuthDTO;
using QuickTix.Mobile.Helpers;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace QuickTix.Mobile.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(UserLoginDTO loginDto);
        string? GetToken();
        UserDTO? GetCurrentUser();
        void Logout();
        Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
    }

    public sealed class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenStore _tokenStore;
        private readonly IAppSession _session;

        private UserDTO? _currentUser;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AuthService(HttpClient httpClient, ITokenStore tokenStore, IAppSession session)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }
        public async Task<bool> LoginAsync(UserLoginDTO loginDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/User/login", loginDto, JsonOptions);
            if (!response.IsSuccessStatusCode)
                return false;

            var api = await response.Content.ReadFromJsonAsync<ApiResponse<UserLoginResponseDTO>>(JsonOptions);
            if (api == null || !api.IsSuccess || api.Result == null)
                return false;

            if (string.IsNullOrWhiteSpace(api.Result.Token))
                return false;

            _tokenStore.SetToken(api.Result.Token);
            _currentUser = api.Result.User;

            // Cargar claims relevantes en sesión
            _session.LoadFromToken(api.Result.Token);

            return true;
        }


        public string? GetToken() => _tokenStore.GetToken();

        public UserDTO? GetCurrentUser() => _currentUser;

        public void Logout()
        {
            _tokenStore.Clear();
            _session.Clear();
            _currentUser = null;
        }

        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            var token = _tokenStore.GetToken();
            if (string.IsNullOrWhiteSpace(token))
                return false;

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var dto = new ChangePasswordRequestDTO
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            };

            var response = await _httpClient.PostAsJsonAsync("api/User/change-password", dto, JsonOptions);
            if (!response.IsSuccessStatusCode)
                return false;

            var raw = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(raw))
                return response.IsSuccessStatusCode;

            try
            {
                var apiBool = JsonSerializer.Deserialize<ApiResponse<bool>>(raw, JsonOptions);
                if (apiBool != null)
                    return apiBool.IsSuccess;
            }
            catch
            {
            }

            try
            {
                var apiObj = JsonSerializer.Deserialize<ApiResponse<object>>(raw, JsonOptions);
                if (apiObj != null)
                    return apiObj.IsSuccess;
            }
            catch
            {
            }

            // Si el servidor devolvió algo inesperado pero el HTTP fue 2xx, asumimos true.
            return true;
        }
    }
}
