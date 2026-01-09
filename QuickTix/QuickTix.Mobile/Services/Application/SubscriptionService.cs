using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using QuickTix.Contracts.Common;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Contracts.Routes;
using QuickTix.Mobile.Helpers;

namespace QuickTix.Mobile.Services
{
    /// <summary>
    /// Contrato del servicio de suscripciones para el cliente móvil.
    /// Permite consultar suscripciones asociadas a un cliente autenticado.
    /// </summary>
    public interface ISubscriptionService
    {
        /// <summary>
        /// Obtiene las suscripciones asociadas a un cliente.
        /// </summary>
        /// <param name="clientId">Identificador del cliente.</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>Listado de suscripciones.</returns>
        Task<List<SubscriptionDTO>> GetByClientAsync(int clientId, CancellationToken ct = default);
    }

    /// <summary>
    /// Implementación HTTP del servicio de suscripciones para el cliente móvil.
    /// Gestiona cabecera Bearer y deserializa el contrato ApiResponse.
    /// </summary>
    public sealed class SubscriptionService : ISubscriptionService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenStore _tokenStore;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="SubscriptionService"/>.
        /// </summary>
        /// <param name="httpClient">HttpClient configurado con la BaseAddress de la API.</param>
        /// <param name="tokenStore">Almacén del token JWT.</param>
        public SubscriptionService(HttpClient httpClient, ITokenStore tokenStore)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        }

        /// <summary>
        /// Obtiene las suscripciones asociadas a un cliente.
        /// Envía el token JWT en cabecera Authorization y devuelve el resultado deserializado.
        /// </summary>
        /// <param name="clientId">Identificador del cliente.</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>Listado de suscripciones.</returns>
        public async Task<List<SubscriptionDTO>> GetByClientAsync(int clientId, CancellationToken ct = default)
        {
            var token = _tokenStore.GetToken();
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("No hay token. Inicia sesión de nuevo.");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                ApiRoutes.Subscription.ByClientId(clientId));

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
}
