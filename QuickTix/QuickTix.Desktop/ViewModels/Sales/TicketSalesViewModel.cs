using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTix.Contracts.DTOs.SaleDTOs.Ticket;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace QuickTix.Desktop.ViewModels.Sales
{
    public partial class TicketSalesViewModel : BaseCrudViewModel<TicketSaleDTO, CreateSaleDTO>
    {
        [ObservableProperty] private ObservableCollection<ManagerDTO> managers = [];
        [ObservableProperty] private ManagerDTO? selectedManager;

        protected override string Endpoint => "Sale/history/tickets";

        [ObservableProperty] private bool isDetailVisible;
        [ObservableProperty] private string detailHeader = string.Empty;
        [ObservableProperty] private ObservableCollection<TicketSaleDetailLineDTO> detailLines = [];

        [ObservableProperty] private string? invitedByClientName;


        public TicketSalesViewModel(HttpJsonClient httpClient) : base(httpClient)
        {
            PropertyChanged += OnSelfPropertyChanged;
            _ = LoadAsync();
        }

        private void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedItem))
            {
                _ = LoadDetailForSelectedAsync(SelectedItem);
            }
        }

        private async Task LoadDetailForSelectedAsync(TicketSaleDTO? selected)
        {
            DetailLines = [];
            DetailHeader = string.Empty;
            InvitedByClientName = null;
            IsDetailVisible = false;

            if (selected == null)
                return;

            try
            {
                var detail = await _httpClient.GetAsync<TicketSaleDetailDTO>(
                    $"api/Sale/history/tickets/{selected.Id}/detail"
                );

                if (detail == null)
                {
                    MessageBox.Show($"La API devolvió detalle nulo para la venta {selected.Id}.");
                    return;
                }

                DetailHeader =
                    $"Venta {detail.Id} | {detail.Date:dd/MM/yyyy HH:mm} | {detail.VenueName} | {detail.ManagerName} | Entradas={detail.Quantity} | Total={detail.TotalAmount}";

                DetailLines = new ObservableCollection<TicketSaleDetailLineDTO>(detail.Lines);

                // Línea "Invitado por"
                if (!string.IsNullOrWhiteSpace(detail.InvitedByClientName))
                    InvitedByClientName = string.IsNullOrWhiteSpace(detail.InvitedByClientName)
    ? null
    : detail.InvitedByClientName;


                IsDetailVisible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando detalle de la venta {selected.Id}: {ex.Message}");
            }
        }

        [RelayCommand]
        private Task CloseDetailAsync()
        {
            IsDetailVisible = false;
            return Task.CompletedTask;
        }
    }
}
