using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTix.Desktop.Models.DTOs;
using QuickTix.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace QuickTix.Desktop.ViewModels
{
    public partial class ClientsViewModel : BaseCrudViewModel<ClientDTO, CreateClientDTO>
    {
        protected override string Endpoint => "Client";

        // Flyout Cliente
        [ObservableProperty] private bool isClientFlyoutOpen;
        [ObservableProperty] private bool isEditingClient;
        [ObservableProperty] private object? activeClientForm;

        // Sección Abonos (mínimo para que no falle el XAML)
        public SubscriptionsViewModel SubscriptionsVM { get; }

        [ObservableProperty] private bool isSubscriptionFlyoutOpen;
        [ObservableProperty] private bool isEditingSubscription;
        [ObservableProperty] private object? activeSubscriptionForm;

        public ObservableCollection<object> SubscriptionCategories { get; } = new();
        public ObservableCollection<object> SubscriptionDurations { get; } = new();

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
        private void OpenSubscriptionFlyout()
        {
            if (SelectedItem == null) return;

            IsEditingSubscription = false;
            ActiveSubscriptionForm = new object(); // luego lo sustituyes por CreateSubscriptionDTO real
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
        private Task SaveSubscription()
        {
            // Placeholder hasta implementar CreateSubscriptionDTO + endpoint
            IsSubscriptionFlyoutOpen = false;
            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task CancelSubscription()
        {
            // Placeholder hasta implementar cancelación real
            return Task.CompletedTask;
        }
    }

}
