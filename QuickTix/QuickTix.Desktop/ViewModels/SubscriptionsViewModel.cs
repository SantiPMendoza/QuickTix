using CommunityToolkit.Mvvm.ComponentModel;
using QuickTix.Contracts.DTOs.SaleDTOs.Subscription;
using QuickTix.Contracts.Enums;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows;

namespace QuickTix.Desktop.ViewModels
{
    public partial class SubscriptionsViewModel : BaseCrudViewModel<SubscriptionDTO, CreateSubscriptionDTO>
    {
        protected override string Endpoint => "Subscription";

        [ObservableProperty] private ObservableCollection<VenueDTO> venues = [];
        [ObservableProperty] private VenueDTO? selectedVenue;

        public ObservableCollection<SubscriptionCategory> Categories { get; } =
            new ObservableCollection<SubscriptionCategory>(Enum.GetValues<SubscriptionCategory>());

        public ObservableCollection<SubscriptionDuration> Durations { get; } =
            new ObservableCollection<SubscriptionDuration>(Enum.GetValues<SubscriptionDuration>());

        public int? CurrentClientId { get; private set; }

        public SubscriptionsViewModel(HttpJsonClient httpClient) : base(httpClient)
        {
        }

        public async Task LoadVenuesAsync()
        {
            try
            {
                Venues = new ObservableCollection<VenueDTO>(
                    await _httpClient.GetListAsync<VenueDTO>("api/Venue"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando recintos: {ex.Message}");
            }
        }

        public async Task LoadByClientAsync(int clientId)
        {
            CurrentClientId = clientId;

            try
            {
                var list = await _httpClient.GetListAsync<SubscriptionDTO>($"api/Subscription/by-client/{clientId}");
                Items = new ObservableCollection<SubscriptionDTO>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando abonos del cliente: {ex.Message}");
            }
        }

        // CRUD administrativo
        public override async Task AddAsync(CreateSubscriptionDTO newItem)
        {
            await base.AddAsync(newItem);

            if (CurrentClientId.HasValue)
                await LoadByClientAsync(CurrentClientId.Value);
        }

        public override async Task DeleteAsync(int id)
        {
            await base.DeleteAsync(id);

            if (CurrentClientId.HasValue)
                await LoadByClientAsync(CurrentClientId.Value);
        }

        public async Task SellAsync(SellSubscriptionDTO request)
        {
            try
            {
                await _httpClient.PostAsync<SellSubscriptionDTO, SaleDTO>(
                "api/Sale/sell/subscription",
                request
                );


                // Recarga listado de abonos del cliente tras la venta
                await LoadByClientAsync(request.ClientId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error registrando la venta del abono: {ex.Message}");
            }
        }
    }
}
