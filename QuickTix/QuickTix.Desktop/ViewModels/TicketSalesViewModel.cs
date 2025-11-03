using QuickTix.Desktop.Models.DTOs;
using QuickTix.Desktop.Models.DTOs.SaleDTO;
using QuickTix.Desktop.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Desktop.ViewModels
{
    public partial class TicketSalesViewModel : BaseCrudViewModel<TicketSaleDTO, CreateSaleDTO>
    {
        [ObservableProperty] private ObservableCollection<ManagerDTO> managers = [];
        [ObservableProperty] private ManagerDTO? selectedManager;
        protected override string Endpoint => "SaleItem/tickets";

        public TicketSalesViewModel(HttpJsonClient httpClient) : base(httpClient)
        {
            _ = LoadAsync();
        }
    }
}
