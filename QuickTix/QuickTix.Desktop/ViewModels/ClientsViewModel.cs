using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTix.Desktop.Models.Forms;
using QuickTix.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using QuickTix.Contracts.Enums;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Contracts.DTOs.SaleDTOs.Subscription;

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
            ErrorMessage = null;
            IsClientFlyoutOpen = true;
        }

        [RelayCommand]
        private void EditClient()
        {
            if (SelectedItem == null) return;

            IsEditingClient = true;
            ErrorMessage = null;

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

            ErrorMessage = null;

            bool ok;
            int? idToReselect = null;

            if (!IsEditingClient)
            {
                ok = await TryAddAsync((CreateClientDTO)ActiveClientForm);
            }
            else
            {
                var dto = (ClientDTO)ActiveClientForm;
                idToReselect = dto.Id;
                ok = await TryUpdateAsync(dto.Id, dto);
            }

            if (!ok)
            {
                // Mantener abierto y mantener datos
                IsClientFlyoutOpen = true;
                return;
            }

            // Éxito: re-seleccionar si aplica
            if (idToReselect.HasValue)
                SelectedItem = Items.FirstOrDefault(x => x.Id == idToReselect.Value);

            // Limpiar y cerrar
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
            ErrorMessage = null;
        }

        // Abonos (stubs)
        [RelayCommand]
        private async Task OpenSubscriptionFlyout()
        {
            if (SelectedItem == null) return;

            IsEditingSubscription = false;

            SubscriptionsVM.ErrorMessage = null;

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

            SubscriptionsVM.ErrorMessage = null;
        }


        [RelayCommand]
        private async Task SaveSubscription()
        {
            if (SelectedItem == null) return;
            if (ActiveSubscriptionForm is not SubscriptionFormModel form) return;

            if (CurrentManagerId <= 0)
            {
                // Esto también podría pasar a ErrorMessage si quieres 100% inline
                SubscriptionsVM.ErrorMessage = "No hay Manager asignado para registrar la venta. Define CurrentManagerId (sesión/login).";
                IsSubscriptionFlyoutOpen = true;
                return;
            }

            SubscriptionsVM.ErrorMessage = null;

            var request = new SellSubscriptionDTO
            {
                ClientId = SelectedItem.Id,
                VenueId = form.VenueId,
                ManagerId = CurrentManagerId,
                Category = form.Category,
                Duration = form.Duration,
                StartDate = form.StartDate,

                // El backend calcula el precio; si falta en el mapa, devolverá error controlado
                Price = 0m
            };

            var ok = await SubscriptionsVM.TrySellAsync(request);

            if (!ok)
            {
                // Mantener flyout y datos
                IsSubscriptionFlyoutOpen = true;
                return;
            }

            // Éxito: cerrar y limpiar
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
