using QuickTix.Contracts.Common;
using QuickTix.Contracts.DTOs.UserAuthDTO;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Contracts.Routes;
using QuickTix.Mobile.Helpers;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace QuickTix.Mobile.Services
{
    /// <summary>
    /// Contrato del servicio de autenticación para el cliente móvil.
    /// Gestiona login, cambio de contraseña, token actual y estado de sesión asociado.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Realiza el login contra la API y, si es correcto, persiste el token y carga la sesión.
        /// </summary>
        /// <param name="loginDto">Credenciales de acceso.</param>
        /// <returns>True si el login fue correcto; false en caso contrario.</returns>
        Task<bool> LoginAsync(UserLoginDTO loginDto);

        string? GetToken();

        UserDTO? GetCurrentUser();

        /// <summary>
        /// Limpia token, sesión y cabeceras de autenticación.
        /// </summary>
        void Logout();

        /// <summary>
        /// Solicita el cambio de contraseña del usuario autenticado.
        /// </summary>
        /// <param name="currentPassword">Contraseña actual.</param>
        /// <param name="newPassword">Nueva contraseña.</param>
        /// <returns>True si el cambio se realizó correctamente; false en caso contrario.</returns>
        Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
    }

    /// <summary>
    /// Servicio de autenticación para el cliente móvil basado en HttpClient.
    /// Persiste el token JWT en <see cref="ITokenStore"/> y carga claims relevantes en <see cref="IAppSession"/>.
    /// </summary>
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

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="AuthService"/>.
        /// </summary>
        /// <param name="httpClient">HttpClient configurado con la BaseAddress de la API.</param>
        /// <param name="tokenStore">Almacén del token JWT.</param>
        /// <param name="session">Sesión de aplicación derivada de claims.</param>
        public AuthService(HttpClient httpClient, ITokenStore tokenStore, IAppSession session)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>
        /// Realiza el login contra la API.
        /// Si es correcto, guarda el token, actualiza el usuario actual y carga la sesión desde el JWT.
        /// </summary>
        /// <param name="loginDto">Credenciales de acceso.</param>
        /// <returns>True si el login fue correcto; false en caso contrario.</returns>
        public async Task<bool> LoginAsync(UserLoginDTO loginDto)
        {
            var response = await _httpClient.PostAsJsonAsync(ApiRoutes.User.Login, loginDto, JsonOptions);
            if (!response.IsSuccessStatusCode)
                return false;

            var api = await response.Content.ReadFromJsonAsync<ApiResponse<UserLoginResponseDTO>>(JsonOptions);
            if (api == null || !api.IsSuccess || api.Result == null)
                return false;

            if (string.IsNullOrWhiteSpace(api.Result.Token))
                return false;

            _tokenStore.SetToken(api.Result.Token);
            _currentUser = api.Result.User;

            _session.LoadFromToken(api.Result.Token);

            return true;
        }

        public string? GetToken() => _tokenStore.GetToken();

        public UserDTO? GetCurrentUser() => _currentUser;

        /// <summary>
        /// Limpia token y sesión, y elimina cabeceras de autenticación del HttpClient.
        /// </summary>
        public void Logout()
        {
            _tokenStore.Clear();
            _session.Clear();
            _currentUser = null;

            _httpClient.DefaultRequestHeaders.Authorization = null;
            if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
        }

        /// <summary>
        /// Solicita a la API el cambio de contraseña del usuario autenticado.
        /// Si la API devuelve <see cref="ApiResponse{T}"/>, interpreta <c>IsSuccess</c>. En caso contrario, usa el status HTTP.
        /// </summary>
        /// <param name="currentPassword">Contraseña actual.</param>
        /// <param name="newPassword">Nueva contraseña.</param>
        /// <returns>True si la operación fue correcta; false en caso contrario.</returns>
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

            var response = await _httpClient.PostAsJsonAsync(ApiRoutes.User.ChangePassword, dto, JsonOptions);
            if (!response.IsSuccessStatusCode)
                return false;

            var raw = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(raw))
                return true;

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

            return true;
        }
    }
}
