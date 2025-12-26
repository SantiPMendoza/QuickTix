using CommunityToolkit.Mvvm.ComponentModel;
using QuickTix.Desktop.Models.DTOs;
using QuickTix.Desktop.Models.DTOs.SaleDTO;
using QuickTix.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;

namespace QuickTix.Desktop.ViewModels.Sales
{
    public partial class SuscriptionSalesViewModel : BaseCrudViewModel<SubscriptionSaleDTO, CreateSaleDTO>
    {
        [ObservableProperty] private ObservableCollection<ManagerDTO> managers = [];
        [ObservableProperty] private ManagerDTO? selectedManager;

        // Nuevo endpoint de historial (SaleController)
        protected override string Endpoint => "Sale/history/subscriptions";

        public SuscriptionSalesViewModel(HttpJsonClient httpClient) : base(httpClient)
        {
            _ = LoadAsync();
        }
    }
}
