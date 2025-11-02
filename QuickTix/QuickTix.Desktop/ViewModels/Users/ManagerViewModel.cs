using QuickTix.Desktop.Models.DTOs;
using QuickTix.Desktop.ViewModels.Base;

namespace QuickTix.Desktop.ViewModels.Users
{
    public partial class ManagerViewModel : BaseCrudViewModel<ManagerDTO, CreateManagerDTO>
    {
        protected override string Endpoint => "Manager";

        public ManagerViewModel(HttpJsonClient httpClient) : base(httpClient)
        {
            _ = LoadAsync();
        }
    }
}
