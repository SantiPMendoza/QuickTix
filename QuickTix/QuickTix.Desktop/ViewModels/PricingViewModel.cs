
using QuickTix.Contracts.Models.DTOs.Pricing;
using QuickTix.Desktop.ViewModels.Base;

namespace QuickTix.Desktop.ViewModels.Pricing
{
    /// <summary>
    /// ViewModel de la pantalla de pricing.
    /// Permite cargar y guardar el mapa de precios por recinto (tickets y suscripciones),
    /// controlando el estado de carga y mostrando mensajes informativos o de error.
    /// </summary>
    public partial class PricingViewModel : ViewModel
    {
        private readonly HttpJsonClient _httpClient;

        [ObservableProperty] private ObservableCollection<VenueDTO> venues = [];
        [ObservableProperty] private VenueDTO? selectedVenue;

        // Evita recargas innecesarias del listado de recintos durante la navegación
        private bool _venuesLoaded;

        [ObservableProperty] private ObservableCollection<VenueTicketPriceDTO> ticketPrices = [];
        [ObservableProperty] private ObservableCollection<VenueSubscriptionPriceDTO> subscriptionPrices = [];

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;
        [ObservableProperty] private string? infoMessage;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="PricingViewModel"/>.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP para consumo de la API.</param>
        public PricingViewModel(HttpJsonClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Hook de navegación: limpia mensajes y carga el listado de recintos (una sola vez).
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        public override async Task OnNavigatedToAsync()
        {
            await base.OnNavigatedToAsync();

            ErrorMessage = null;
            InfoMessage = null;

            await LoadVenuesAsync();
        }

        /// <summary>
        /// Carga el listado de recintos desde la API.
        /// Mantiene una caché local simple mediante <c>_venuesLoaded</c>.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        public async Task LoadVenuesAsync()
        {
            if (_venuesLoaded)
                return;

            try
            {
                IsBusy = true;

                Venues = new ObservableCollection<VenueDTO>(
                    await _httpClient.GetListAsync<VenueDTO>(ApiRoutes.Venue.GetAll));

                if (SelectedVenue == null && Venues.Count > 0)
                    SelectedVenue = Venues[0];

                _venuesLoaded = true;
            }
            catch (ApiException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error cargando recintos: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Carga el mapa de precios del recinto seleccionado (tickets y suscripciones).
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        private async Task LoadPriceMapAsync()
        {
            ErrorMessage = null;
            InfoMessage = null;

            if (SelectedVenue == null)
            {
                ErrorMessage = "Selecciona un recinto.";
                return;
            }

            try
            {
                IsBusy = true;

                var venueId = SelectedVenue.Id;

                var map = await _httpClient.GetAsync<VenuePriceMapDTO>(
                    ApiRoutes.Pricing.GetVenuePriceMapByVenueId(venueId));

                if (map == null)
                {
                    ErrorMessage = "El servidor no devolvió datos del mapa de precios.";
                    return;
                }

                TicketPrices = new ObservableCollection<VenueTicketPriceDTO>(map.TicketPrices);
                SubscriptionPrices = new ObservableCollection<VenueSubscriptionPriceDTO>(map.SubscriptionPrices);

                InfoMessage = "Mapa de precios cargado.";
            }
            catch (ApiException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error cargando el mapa de precios: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Guarda el mapa de precios del recinto seleccionado.
        /// Valida precios negativos y envía un payload de upsert a la API.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        private async Task SavePriceMapAsync()
        {
            ErrorMessage = null;
            InfoMessage = null;

            if (SelectedVenue == null)
            {
                ErrorMessage = "Selecciona un recinto.";
                return;
            }

            if (TicketPrices.Any(x => x.Price < 0) || SubscriptionPrices.Any(x => x.Price < 0))
            {
                ErrorMessage = "No se permiten precios negativos.";
                return;
            }

            try
            {
                IsBusy = true;

                var venueId = SelectedVenue.Id;

                var payload = new UpsertVenuePriceMapDTO
                {
                    VenueId = venueId,
                    TicketPrices = TicketPrices
                        .Select(x => new VenueTicketPriceDTO
                        {
                            VenueId = venueId,
                            Type = x.Type,
                            Context = x.Context,
                            Price = x.Price
                        })
                        .ToList(),
                    SubscriptionPrices = SubscriptionPrices
                        .Select(x => new VenueSubscriptionPriceDTO
                        {
                            VenueId = venueId,
                            Category = x.Category,
                            Duration = x.Duration,
                            Price = x.Price
                        })
                        .ToList()
                };

                await _httpClient.PutAsync(
                    ApiRoutes.Pricing.UpsertVenuePriceMapByVenueId(venueId),
                    payload);

                InfoMessage = "Mapa de precios guardado correctamente.";
            }
            catch (ApiException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error guardando el mapa de precios: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Limpia los mensajes informativos y de error mostrados en la UI.
        /// </summary>
        [RelayCommand]
        private void ClearMessages()
        {
            ErrorMessage = null;
            InfoMessage = null;
        }
    }
}
