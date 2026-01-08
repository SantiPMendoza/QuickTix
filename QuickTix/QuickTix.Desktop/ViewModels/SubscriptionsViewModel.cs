
using QuickTix.Contracts.DTOs.SaleDTOs.Subscription;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Desktop.ViewModels.Base;

namespace QuickTix.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel de suscripciones (abonos).
    /// Permite cargar suscripciones por cliente, realizar operaciones CRUD y registrar la venta de una suscripción.
    /// </summary>
    public partial class SubscriptionsViewModel : BaseCrudViewModel<SubscriptionDTO, CreateSubscriptionDTO>
    {
        protected override string Endpoint => "Subscription";

        [ObservableProperty] private ObservableCollection<VenueDTO> venues = [];
        [ObservableProperty] private VenueDTO? selectedVenue;

        /// <summary>
        /// Categorías disponibles para la creación/venta de suscripciones.
        /// </summary>
        public ObservableCollection<SubscriptionCategory> Categories { get; } =
            new ObservableCollection<SubscriptionCategory>(Enum.GetValues<SubscriptionCategory>());

        /// <summary>
        /// Duraciones disponibles para la creación/venta de suscripciones.
        /// </summary>
        public ObservableCollection<SubscriptionDuration> Durations { get; } =
            new ObservableCollection<SubscriptionDuration>(Enum.GetValues<SubscriptionDuration>());

        /// <summary>
        /// Cliente actualmente cargado en contexto (si el listado está filtrado por cliente).
        /// </summary>
        public int? CurrentClientId { get; private set; }

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="SubscriptionsViewModel"/>.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP para consumo de la API.</param>
        public SubscriptionsViewModel(HttpJsonClient httpClient) : base(httpClient)
        {
        }

        /// <summary>
        /// Carga el listado de recintos desde la API para poblar selectores en la UI.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        public async Task LoadVenuesAsync()
        {
            try
            {
                Venues = new ObservableCollection<VenueDTO>(
                    await _httpClient.GetListAsync<VenueDTO>(ApiRoutes.Venue.GetAll));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando recintos: {ex.Message}");
            }
        }

        /// <summary>
        /// Carga las suscripciones asociadas a un cliente.
        /// Establece el contexto de cliente actual para recargas posteriores.
        /// </summary>
        /// <param name="clientId">Identificador del cliente.</param>
        /// <returns>Tarea asíncrona.</returns>
        public async Task LoadByClientAsync(int clientId)
        {
            CurrentClientId = clientId;

            try
            {
                var list = await _httpClient.GetListAsync<SubscriptionDTO>(
                    ApiRoutes.Subscription.ByClientId(clientId));

                Items = new ObservableCollection<SubscriptionDTO>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando abonos del cliente: {ex.Message}");
            }
        }

        /// <summary>
        /// Crea una suscripción mediante el flujo CRUD estándar y recarga el listado del cliente si existe contexto.
        /// </summary>
        /// <param name="newItem">DTO de creación.</param>
        /// <returns>Tarea asíncrona.</returns>
        public override async Task AddAsync(CreateSubscriptionDTO newItem)
        {
            await base.AddAsync(newItem);

            if (CurrentClientId.HasValue)
                await LoadByClientAsync(CurrentClientId.Value);
        }

        /// <summary>
        /// Elimina una suscripción mediante el flujo CRUD estándar y recarga el listado del cliente si existe contexto.
        /// </summary>
        /// <param name="id">Identificador de la suscripción.</param>
        /// <returns>Tarea asíncrona.</returns>
        public override async Task DeleteAsync(int id)
        {
            await base.DeleteAsync(id);

            if (CurrentClientId.HasValue)
                await LoadByClientAsync(CurrentClientId.Value);
        }

        /// <summary>
        /// Registra la venta de una suscripción y recarga el listado del cliente asociado.
        /// Muestra MessageBox en caso de error.
        /// </summary>
        /// <param name="request">Datos de la venta de suscripción.</param>
        /// <returns>Tarea asíncrona.</returns>
        public async Task SellAsync(SellSubscriptionDTO request)
        {
            try
            {
                await _httpClient.PostAsync<SellSubscriptionDTO, SaleDTO>(
                    ApiRoutes.Sale.SellSubscription,
                    request);

                await LoadByClientAsync(request.ClientId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error registrando la venta del abono: {ex.Message}");
            }
        }

        /// <summary>
        /// Registra la venta de una suscripción sin mostrar MessageBox.
        /// Devuelve false y deja <see cref="BaseCrudViewModel{T, TCreate}.ErrorMessage"/> preparado para UI inline.
        /// </summary>
        /// <param name="request">Datos de la venta de suscripción.</param>
        /// <returns>
        /// True si la venta se registró correctamente; false si se produjo un error controlado.
        /// </returns>
        public async Task<bool> TrySellAsync(SellSubscriptionDTO request)
        {
            try
            {
                ErrorMessage = null;

                await _httpClient.PostAsync<SellSubscriptionDTO, SaleDTO>(
                    ApiRoutes.Sale.SellSubscription,
                    request);

                await LoadByClientAsync(request.ClientId);
                return true;
            }
            catch (ApiException apiEx)
            {
                ErrorMessage =
                    $"No se pudo registrar la venta del abono.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}";
                return false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error local registrando la venta del abono: {ex.Message}";
                return false;
            }
        }
    }
}
