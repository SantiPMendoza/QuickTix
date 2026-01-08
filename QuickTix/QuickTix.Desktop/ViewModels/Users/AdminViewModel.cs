
using QuickTix.Desktop.ViewModels.Base;

namespace QuickTix.Desktop.ViewModels.Users
{
    /// <summary>
    /// ViewModel CRUD para la gestión de administradores.
    /// </summary>
    public partial class AdminViewModel : BaseCrudViewModel<AdminDTO, CreateAdminDTO>
    {
        protected override string Endpoint => "Admin";

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="AdminViewModel"/> y carga el listado inicial.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP para consumo de la API.</param>
        public AdminViewModel(HttpJsonClient httpClient) : base(httpClient)
        {
            _ = LoadAsync();
        }
    }
}
