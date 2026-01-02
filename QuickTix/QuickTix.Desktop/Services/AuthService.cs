using QuickTix.Contracts.DTOs.UserAuthDTO;

namespace QuickTix.Desktop.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(UserLoginDTO loginDto);
        string? GetToken();
        UserDTO? GetCurrentUser();
        void Logout();
    }

    public class AuthService : IAuthService
    {
        private readonly HttpJsonClient _apiClient;
        private readonly ITokenStore _tokenStore;

        private UserDTO? _currentUser;

        public AuthService(HttpJsonClient apiClient, ITokenStore tokenStore)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        }

        public async Task<bool> LoginAsync(UserLoginDTO loginDto)
        {
            var result = await _apiClient.PostAsync<UserLoginDTO, UserLoginResponseDTO>(
                "api/User/login",
                loginDto
            );

            if (string.IsNullOrWhiteSpace(result.Token))
                return false;

            _tokenStore.SetToken(result.Token);
            _currentUser = result.User;

            return true;
        }

        public string? GetToken() => _tokenStore.GetToken();

        public UserDTO? GetCurrentUser() => _currentUser;

        public void Logout()
        {
            _tokenStore.Clear();
            _currentUser = null;
        }
    }
}
