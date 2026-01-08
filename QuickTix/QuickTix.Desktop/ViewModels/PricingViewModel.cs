using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Contracts.Models.DTOs.Pricing;
using QuickTix.Core.Models.Entities;
using QuickTix.Desktop.Services;
using QuickTix.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;

namespace QuickTix.Desktop.ViewModels.Pricing
{
    public partial class PricingViewModel : ViewModel
    {
        private readonly HttpJsonClient _httpClient;

        [ObservableProperty] private ObservableCollection<VenueDTO> venues = [];
        [ObservableProperty] private VenueDTO? selectedVenue;

        private bool _venuesLoaded;


        [ObservableProperty] private ObservableCollection<VenueTicketPriceDTO> ticketPrices = [];
        [ObservableProperty] private ObservableCollection<VenueSubscriptionPriceDTO> subscriptionPrices = [];

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;
        [ObservableProperty] private string? infoMessage;

        public PricingViewModel(HttpJsonClient httpClient)
        {
            _httpClient = httpClient;
        }

        public override async Task OnNavigatedToAsync()
        {
            await base.OnNavigatedToAsync();

            ErrorMessage = null;
            InfoMessage = null;

            await LoadVenuesAsync();
        }

        public async Task LoadVenuesAsync()
        {
            if (_venuesLoaded)
                return;

            try
            {
                IsBusy = true;

                Venues = new ObservableCollection<VenueDTO>(
                    await _httpClient.GetListAsync<VenueDTO>("api/Venue"));

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

                var map = await _httpClient.GetAsync<VenuePriceMapDTO>($"api/pricing/venue/{venueId}");
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

                await _httpClient.PutAsync($"api/pricing/venue/{venueId}", payload);

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

        [RelayCommand]
        private void ClearMessages()
        {
            ErrorMessage = null;
            InfoMessage = null;
        }
    }
}
