using CommunityToolkit.Mvvm.ComponentModel;
using QuickTix.Desktop.Models.DTOs;
using QuickTix.Desktop.Models.DTOs.SaleDTO;
using QuickTix.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;

namespace QuickTix.Desktop.ViewModels.Sales
{
    public partial class TicketSalesViewModel : BaseCrudViewModel<TicketSaleDTO, CreateSaleDTO>
    {
        [ObservableProperty] private ObservableCollection<ManagerDTO> managers = [];
        [ObservableProperty] private ManagerDTO? selectedManager;

        // Nuevo endpoint de historial (SaleController)
        protected override string Endpoint => "Sale/history/tickets";

        public TicketSalesViewModel(HttpJsonClient httpClient) : base(httpClient)
        {
            _ = LoadAsync();
        }
    }
}
