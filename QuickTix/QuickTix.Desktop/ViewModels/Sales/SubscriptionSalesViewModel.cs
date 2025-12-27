using CommunityToolkit.Mvvm.ComponentModel;
using QuickTix.Desktop.Models.DTOs;
using QuickTix.Desktop.Models.DTOs.SaleDTO;
using QuickTix.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;

namespace QuickTix.Desktop.ViewModels.Sales
{
    public partial class SubscriptionSalesViewModel : BaseCrudViewModel<SubscriptionSaleDTO, CreateSaleDTO>
    {
        [ObservableProperty] private ObservableCollection<ManagerDTO> managers = [];
        [ObservableProperty] private ManagerDTO? selectedManager;


        protected override string Endpoint => "Sale/history/subscriptions";

        public SubscriptionSalesViewModel(HttpJsonClient httpClient) : base(httpClient)
        {
            _ = LoadAsync();
        }
    }
}
