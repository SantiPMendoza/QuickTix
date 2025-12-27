using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTix.Core.Enums;
using QuickTix.Desktop.Models.DTOs.SaleDTO;
using QuickTix.Desktop.Models.DTOs;
using QuickTix.Desktop.Models.Forms;
using QuickTix.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace QuickTix.Desktop.ViewModels
{
    public partial class ClientsViewModel : BaseCrudViewModel<ClientDTO, CreateClientDTO>
    {
        protected override string Endpoint => "Client";

        public int CurrentManagerId { get; set; } = 1;


        // Flyout Cliente
        [ObservableProperty] private bool isClientFlyoutOpen;
        [ObservableProperty] private bool isEditingClient;
        [ObservableProperty] private object? activeClientForm;

        // Sección Abonos (mínimo para que no falle el XAML)
        public SubscriptionsViewModel SubscriptionsVM { get; }

        [ObservableProperty] private bool isSubscriptionFlyoutOpen;
        [ObservableProperty] private bool isEditingSubscription;
        [ObservableProperty] private object? activeSubscriptionForm;
        public ClientsViewModel(HttpJsonClient httpClient) : base(httpClient)
        {
            SubscriptionsVM = new SubscriptionsViewModel(httpClient);

            _ = LoadAsync();
        }

        [RelayCommand]
        private void OpenClientFlyout()
        {
            IsEditingClient = false;
            ActiveClientForm = new CreateClientDTO();
            IsClientFlyoutOpen = true;
        }

        [RelayCommand]
        private void EditClient()
        {
            if (SelectedItem == null) return;

            IsEditingClient = true;
            ActiveClientForm = new ClientDTO
            {
                Id = SelectedItem.Id,
                Name = SelectedItem.Name,
                Email = SelectedItem.Email,
                Nif = SelectedItem.Nif,
                PhoneNumber = SelectedItem.PhoneNumber
            };
            IsClientFlyoutOpen = true;
        }

        [RelayCommand]
        private async Task SaveClient()
        {
            if (ActiveClientForm == null) return;

            int? idToReselect = null;

            if (!IsEditingClient)
            {
                await AddAsync((CreateClientDTO)ActiveClientForm);
            }
            else
            {
                var dto = (ClientDTO)ActiveClientForm;
                idToReselect = dto.Id;
                await UpdateAsync(dto.Id, dto);
            }

            if (idToReselect.HasValue)
                SelectedItem = Items.FirstOrDefault(x => x.Id == idToReselect.Value);

            ActiveClientForm = null;
            IsEditingClient = false;
            IsClientFlyoutOpen = false;
        }

        [RelayCommand]
        private void CloseClientFlyout()
        {
            IsClientFlyoutOpen = false;
            ActiveClientForm = null;
            IsEditingClient = false;
        }

        // Abonos (stubs)
        [RelayCommand]
        private async Task OpenSubscriptionFlyout()
        {
            if (SelectedItem == null) return;

            IsEditingSubscription = false;

            await SubscriptionsVM.LoadVenuesAsync();
            if (SubscriptionsVM.Venues.Count == 0) return;

            SubscriptionsVM.SelectedVenue = SubscriptionsVM.Venues[0];

            ActiveSubscriptionForm = new SubscriptionFormModel
            {
                VenueId = SubscriptionsVM.SelectedVenue.Id,
                Category = SubscriptionCategory.Adulto,
                Duration = SubscriptionDuration.Mensual,
                StartDate = DateTime.Today
            };

            IsSubscriptionFlyoutOpen = true;
        }

        [RelayCommand]
        private void CloseSubscriptionFlyout()
        {
            IsSubscriptionFlyoutOpen = false;
            ActiveSubscriptionForm = null;
            IsEditingSubscription = false;
        }

        [RelayCommand]
        private async Task SaveSubscription()
        {
            if (SelectedItem == null) return;
            if (ActiveSubscriptionForm is not SubscriptionFormModel form) return;

            if (CurrentManagerId <= 0)
            {
                MessageBox.Show("No hay Manager asignado para registrar la venta. Define CurrentManagerId (sesión/login).");
                return;
            }

            var request = new SellSubscriptionDTO
            {
                ClientId = SelectedItem.Id,
                VenueId = form.VenueId,
                ManagerId = CurrentManagerId,
                Category = form.Category,
                Duration = form.Duration,
                StartDate = form.StartDate,
                Price = 0m // que lo calcule backend
            };

            // Llama al flujo de venta en SaleController
            await _httpClient.PostAsync<SellSubscriptionDTO, object>(
                "api/Sale/sell/subscription",
                request
            );

            // Recarga el listado de abonos del cliente (ya incluirá el nuevo)
            await SubscriptionsVM.LoadByClientAsync(SelectedItem.Id);

            ActiveSubscriptionForm = null;
            IsEditingSubscription = false;
            IsSubscriptionFlyoutOpen = false;
        }


        [RelayCommand]
        private async Task CancelSubscription()
        {
            if (SelectedItem == null) return;
            if (SubscriptionsVM.SelectedItem == null) return;

            var subId = SubscriptionsVM.SelectedItem.Id;

            await SubscriptionsVM.DeleteAsync(subId);

            SubscriptionsVM.SelectedItem = null;
        }

        protected override async Task OnSelectedItemChangedAsync(ClientDTO? value)
        {
            if (value == null)
            {
                SubscriptionsVM.Items.Clear();
                SubscriptionsVM.SelectedItem = null;
                return;
            }

            await SubscriptionsVM.LoadByClientAsync(value.Id);
        }



    }

}
