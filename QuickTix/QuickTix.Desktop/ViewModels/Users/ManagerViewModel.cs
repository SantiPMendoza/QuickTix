
using QuickTix.Desktop.ViewModels.Base;


namespace QuickTix.Desktop.ViewModels.Users
{
    /// <summary>
    /// ViewModel CRUD para la gestión de managers.
    /// Incluye carga auxiliar de recintos (venues) para selección en formularios.
    /// </summary>
    public partial class ManagerViewModel : BaseCrudViewModel<ManagerDTO, CreateManagerDTO>
    {
        protected override string Endpoint => "Manager";

        /// <summary>
        /// Recintos disponibles para asociar un manager (usado por la UI en creación/edición).
        /// </summary>
        [ObservableProperty] private ObservableCollection<VenueDTO> venues = [];

        /// <summary>
        /// Recinto seleccionado actualmente en la UI (si aplica).
        /// </summary>
        [ObservableProperty] private VenueDTO? selectedVenue;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ManagerViewModel"/> y carga el listado inicial.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP para consumo de la API.</param>
        public ManagerViewModel(HttpJsonClient httpClient) : base(httpClient)
        {
            _ = LoadAsync();
        }

        /// <summary>
        /// Carga el listado de recintos desde la API para poblar selectores en la UI.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        public async Task LoadVenuesAsync()
        {
            try
            {
                Venues = new ObservableCollection<VenueDTO>(
                    await _httpClient.GetListAsync<VenueDTO>(ApiRoutes.Venue.GetAll));
            }
            catch (Exception ex)
            {
                ShowAlert("Error", $"Error cargando recintos: {ex.Message}");
            }
        }
    }
}
