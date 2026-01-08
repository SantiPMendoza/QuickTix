using QuickTix.Contracts.DTOs.UserAuthDTO;
using QuickTix.Contracts.Routes;

namespace QuickTix.Desktop.Services
{
    /// <summary>
    /// Contrato del servicio de autenticación del cliente Desktop.
    /// Gestiona login, token actual, usuario autenticado y logout.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Realiza el login contra la API y, si tiene éxito, almacena el token y el usuario actual.
        /// </summary>
        /// <param name="loginDto">Credenciales del usuario.</param>
        /// <returns>True si el login fue correcto y se obtuvo token; en caso contrario, false.</returns>
        Task<bool> LoginAsync(UserLoginDTO loginDto);

        /// <summary>
        /// Obtiene el token JWT actual almacenado (si existe).
        /// </summary>
        /// <returns>Token JWT o null.</returns>
        string? GetToken();

        /// <summary>
        /// Obtiene el usuario autenticado actualmente (si existe).
        /// </summary>
        /// <returns>Usuario actual o null.</returns>
        UserDTO? GetCurrentUser();

        /// <summary>
        /// Cierra sesión limpiando token y usuario en memoria.
        /// </summary>
        void Logout();
    }

    /// <summary>
    /// Implementación del servicio de autenticación.
    /// Consumirá la API para login y persistirá el token en un store en memoria.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly HttpJsonClient _apiClient;

        private readonly TokenStore _tokenStore;

        private UserDTO? _currentUser;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="AuthService"/>.
        /// </summary>
        /// <param name="apiClient">Cliente HTTP JSON.</param>
        /// <param name="tokenStore">Store del token JWT.</param>
        public AuthService(HttpJsonClient apiClient, TokenStore tokenStore)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        }

        /// <summary>
        /// Envía las credenciales de login a la API.
        /// Si la respuesta contiene token, lo guarda en el store y almacena el usuario actual.
        /// </summary>
        /// <param name="loginDto">Datos de login.</param>
        /// <returns>True si se recibió un token válido; en caso contrario, false.</returns>
        public async Task<bool> LoginAsync(UserLoginDTO loginDto)
        {
            var result = await _apiClient.PostAsync<UserLoginDTO, UserLoginResponseDTO>(
                ApiRoutes.User.Login,
                loginDto
            );

            if (string.IsNullOrWhiteSpace(result.Token))
                return false;

            _tokenStore.SetToken(result.Token);
            _currentUser = result.User;

            return true;
        }

        /// <summary>
        /// Devuelve el token actual (si existe) desde el store.
        /// </summary>
        /// <returns>Token JWT o null.</returns>
        public string? GetToken() => _tokenStore.GetToken();

        /// <summary>
        /// Devuelve el usuario actualmente autenticado en memoria.
        /// </summary>
        /// <returns>Usuario actual o null.</returns>
        public UserDTO? GetCurrentUser() => _currentUser;

        /// <summary>
        /// Limpia la sesión actual borrando token y usuario.
        /// </summary>
        public void Logout()
        {
            _tokenStore.Clear();
            _currentUser = null;
        }
    }
}
