using QuickTix.Mobile.Models.UserDTO;
using System.Net.Http;
using System.Net.Http.Json;


namespace QuickTix.Mobile.Services;
public interface IAuthService
{
    Task<bool> LoginAsync(UserLoginDTO loginDto);
    string? GetToken();
    UserDTO? GetCurrentUser();
    void Logout();
    Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);

}

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private string? _token;
    private UserDTO? _currentUser;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> LoginAsync(UserLoginDTO loginDto)
    {
        var response = await _httpClient.PostAsJsonAsync("http://localhost:5137/api/User/login", loginDto);

        if (!response.IsSuccessStatusCode)
            return false;

        var loginResponse = await response.Content.ReadFromJsonAsync<UserLoginResponseDTO>();

        if (loginResponse?.IsSuccess == true && loginResponse.Result?.Token is not null)
        {
            _token = loginResponse.Result.Token;
            _currentUser = loginResponse.Result.User;
            return true;
        }

        return false;
    }

    public string? GetToken() => _token;

    public UserDTO? GetCurrentUser() => _currentUser;

    public void Logout()
    {
        _token = null;
        _currentUser = null;
    }

    public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(_token))
            return false;

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

        var dto = new ChangePasswordRequestDTO
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword
        };

        var response = await _httpClient.PostAsJsonAsync("http://localhost:5137/api/User/change-password", dto);
        return response.IsSuccessStatusCode;
    }

}
