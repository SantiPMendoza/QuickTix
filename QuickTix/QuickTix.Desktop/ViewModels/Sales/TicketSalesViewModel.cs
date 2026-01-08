
using QuickTix.Contracts.DTOs.SaleDTOs.Ticket;

using QuickTix.Contracts.Models.DTOs.SaleDTOs;

using QuickTix.Desktop.ViewModels.Base;
using System.ComponentModel;

namespace QuickTix.Desktop.ViewModels.Sales
{
    /// <summary>
    /// ViewModel del histórico de ventas de tickets.
    /// Permite visualizar el detalle de una venta seleccionada.
    /// </summary>
    public partial class TicketSalesViewModel
        : BaseCrudViewModel<TicketSaleDTO, CreateSaleDTO>
    {
        /// <summary>
        /// Listado de managers disponible para la vista.
        /// </summary>
        [ObservableProperty] private ObservableCollection<ManagerDTO> managers = [];

        /// <summary>
        /// Manager seleccionado actualmente en la UI.
        /// </summary>
        [ObservableProperty] private ManagerDTO? selectedManager;

        /// <summary>
        /// Recurso lógico asociado a este ViewModel.
        /// </summary>
        protected override string Endpoint => "Sale";

        /// <summary>
        /// Ruta del listado del histórico de ventas de tickets.
        /// </summary>
        protected override string ListRoute => ApiRoutes.Sale.HistoryTickets;

        /// <summary>
        /// Indica si el panel de detalle está visible.
        /// </summary>
        [ObservableProperty] private bool isDetailVisible;

        /// <summary>
        /// Cabecera descriptiva del detalle de la venta seleccionada.
        /// </summary>
        [ObservableProperty] private string detailHeader = string.Empty;

        /// <summary>
        /// Líneas que componen el detalle de la venta.
        /// </summary>
        [ObservableProperty] private ObservableCollection<TicketSaleDetailLineDTO> detailLines = [];

        /// <summary>
        /// Nombre del cliente que invitó la venta, si aplica.
        /// </summary>
        [ObservableProperty] private string? invitedByClientName;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="TicketSalesViewModel"/>.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP para consumo de la API.</param>
        public TicketSalesViewModel(HttpJsonClient httpClient)
            : base(httpClient)
        {
            PropertyChanged += OnSelfPropertyChanged;
            _ = LoadAsync();
        }

        /// <summary>
        /// Detecta cambios en propiedades del propio ViewModel.
        /// Se usa para reaccionar al cambio de venta seleccionada.
        /// </summary>
        private void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedItem))
                _ = LoadDetailForSelectedAsync(SelectedItem);
        }

        /// <summary>
        /// Carga el detalle completo de la venta de tickets seleccionada
        /// y actualiza las propiedades visibles en la UI.
        /// </summary>
        /// <param name="selected">Venta seleccionada.</param>
        /// <returns>Tarea asíncrona.</returns>
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
                    ApiRoutes.Sale.HistoryTicketDetailBySaleId(selected.Id)
                );

                if (detail == null)
                {
                    ErrorMessage = $"La API devolvió detalle nulo para la venta {selected.Id}.";
                    return;
                }

                DetailHeader =
                    $"Venta {detail.Id} | {detail.Date:dd/MM/yyyy HH:mm} | {detail.VenueName} | {detail.ManagerName} | Entradas={detail.Quantity} | Total={detail.TotalAmount}";

                DetailLines = new ObservableCollection<TicketSaleDetailLineDTO>(detail.Lines);
                InvitedByClientName = string.IsNullOrWhiteSpace(detail.InvitedByClientName)
                    ? null
                    : detail.InvitedByClientName;

                IsDetailVisible = true;
            }
            catch (ApiException apiEx)
            {
                ErrorMessage =
                    $"Error cargando detalle de la venta {selected.Id}.\nCódigo: {(int)apiEx.StatusCode}\nMensaje: {apiEx.Message}";
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    $"Error local cargando detalle de la venta {selected.Id}: {ex.Message}";
            }
        }

        /// <summary>
        /// Oculta el panel de detalle de la venta.
        /// </summary>
        /// <returns>Tarea completada.</returns>
        [RelayCommand]
        private Task CloseDetailAsync()
        {
            IsDetailVisible = false;
            return Task.CompletedTask;
        }
    }
}
