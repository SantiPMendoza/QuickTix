using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using QuickTix.Contracts.Common;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Mobile.Helpers;

namespace QuickTix.Mobile.Services;

public interface ISubscriptionService
{
    Task<List<SubscriptionDTO>> GetByClientAsync(int clientId, CancellationToken ct = default);
}

public sealed class SubscriptionService : ISubscriptionService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenStore _tokenStore;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SubscriptionService(HttpClient httpClient, ITokenStore tokenStore)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    }

    public async Task<List<SubscriptionDTO>> GetByClientAsync(int clientId, CancellationToken ct = default)
    {
        var token = _tokenStore.GetToken();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("No hay token. Inicia sesión de nuevo.");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/Subscription/by-client/{clientId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync(ct);
            throw new Exception($"HTTP {(int)response.StatusCode}: {raw}");
        }

        var api = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<SubscriptionDTO>>>(JsonOptions, ct);
        return api?.Result?.ToList() ?? new List<SubscriptionDTO>();
    }
}
