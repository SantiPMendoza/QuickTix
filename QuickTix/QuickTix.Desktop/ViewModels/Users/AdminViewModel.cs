using QuickTix.Desktop.Models.DTOs;
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

        // Si más adelante necesitas algo específico (filtros, roles, etc.)
        // puedes extender con nuevos métodos sin romper la base.
    }
}
