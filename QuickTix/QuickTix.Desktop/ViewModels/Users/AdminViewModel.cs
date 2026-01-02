using QuickTix.Contracts.Models.DTOs;
using QuickTix.Desktop.ViewModels.Base;

namespace QuickTix.Desktop.ViewModels.Users
{
    public partial class AdminViewModel : BaseCrudViewModel<AdminDTO, CreateAdminDTO>
    {
        protected override string Endpoint => "Admin";

        public AdminViewModel(HttpJsonClient httpClient) : base(httpClient)
        {
            _ = LoadAsync();
        }
    }
}
