
using QuickTix.Desktop.ViewModels.Base;

namespace QuickTix.Desktop.ViewModels.Users
{
    /// <summary>
    /// ViewModel de la pantalla de usuarios.
    /// Coordina la gestión de administradores y managers, incluyendo formularios en flyouts
    /// para creación y edición.
    /// </summary>
    public partial class UsersViewModel : ViewModel
    {
        /// <summary>
        /// Módulo CRUD de administradores.
        /// </summary>
        public AdminViewModel AdminsVM { get; }

        /// <summary>
        /// Módulo CRUD de managers.
        /// </summary>
        public ManagerViewModel ManagersVM { get; }

        // Estado de flyouts (UI)
        [ObservableProperty] private bool isAdminFlyoutOpen;
        [ObservableProperty] private bool isManagerFlyoutOpen;

        // Estados de edición
        [ObservableProperty] private bool isEditingAdmin;
        [ObservableProperty] private bool isEditingManager;

        // Formularios activos (crear o editar); se tipan como object para reutilizar plantilla en UI
        [ObservableProperty] private object? activeAdminForm;
        [ObservableProperty] private object? activeManagerForm;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="UsersViewModel"/> y crea los módulos CRUD.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP para consumo de la API.</param>
        public UsersViewModel(HttpJsonClient httpClient)
        {
            AdminsVM = new AdminViewModel(httpClient);
            ManagersVM = new ManagerViewModel(httpClient);
        }

        /// <summary>
        /// Abre el flyout de administradores en modo creación, inicializando el formulario.
        /// </summary>
        [RelayCommand]
        private void OpenAdminFlyout()
        {
            IsEditingAdmin = false;
            ActiveAdminForm = new CreateAdminDTO();

            // Limpia errores previos para no arrastrarlos al nuevo formulario
            AdminsVM.ErrorMessage = null;

            IsAdminFlyoutOpen = true;
        }

        /// <summary>
        /// Abre el flyout de administradores en modo edición, clonando el seleccionado a un formulario editable.
        /// </summary>
        [RelayCommand]
        private void EditAdmin()
        {
            if (AdminsVM.SelectedItem == null)
                return;

            IsEditingAdmin = true;

            ActiveAdminForm = new AdminDTO
            {
                Id = AdminsVM.SelectedItem.Id,
                Name = AdminsVM.SelectedItem.Name,
                Email = AdminsVM.SelectedItem.Email,
                Nif = AdminsVM.SelectedItem.Nif,
                PhoneNumber = AdminsVM.SelectedItem.PhoneNumber
            };

            AdminsVM.ErrorMessage = null;
            IsAdminFlyoutOpen = true;
        }

        /// <summary>
        /// Guarda el formulario de administradores (creación o edición) usando el módulo CRUD.
        /// Si falla, mantiene el flyout abierto y conserva los datos introducidos.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        private async Task SaveAdmin()
        {
            if (ActiveAdminForm == null)
                return;

            bool ok;

            if (!IsEditingAdmin)
            {
                ok = await AdminsVM.TryAddAsync((CreateAdminDTO)ActiveAdminForm);
            }
            else
            {
                var dto = (AdminDTO)ActiveAdminForm;
                ok = await AdminsVM.TryUpdateAsync(dto.Id, dto);
            }

            if (!ok)
            {
                // Mantener abierto y conservar datos para corregir errores
                IsAdminFlyoutOpen = true;
                return;
            }

            // Solo si éxito: limpiar y cerrar
            ActiveAdminForm = null;
            IsEditingAdmin = false;
            IsAdminFlyoutOpen = false;
        }

        /// <summary>
        /// Cierra el flyout de administradores y limpia el estado del formulario.
        /// </summary>
        [RelayCommand]
        private void CloseAdminFlyout()
        {
            ActiveAdminForm = null;
            IsEditingAdmin = false;
            AdminsVM.ErrorMessage = null;

            IsAdminFlyoutOpen = false;
        }

        /// <summary>
        /// Abre el flyout de managers en modo creación, precargando recintos para el selector.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        private async Task OpenManagerFlyout()
        {
            IsEditingManager = false;
            ActiveManagerForm = new CreateManagerDTO();

            ManagersVM.ErrorMessage = null;

            await ManagersVM.LoadVenuesAsync();
            IsManagerFlyoutOpen = true;
        }

        /// <summary>
        /// Abre el flyout de managers en modo edición, precargando recintos y clonando el seleccionado.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        private async Task EditManager()
        {
            if (ManagersVM.SelectedItem == null)
                return;

            IsEditingManager = true;
            ManagersVM.ErrorMessage = null;

            await ManagersVM.LoadVenuesAsync();

            ActiveManagerForm = new ManagerDTO
            {
                Id = ManagersVM.SelectedItem.Id,
                Name = ManagersVM.SelectedItem.Name,
                Email = ManagersVM.SelectedItem.Email,
                Nif = ManagersVM.SelectedItem.Nif,
                PhoneNumber = ManagersVM.SelectedItem.PhoneNumber,
                VenueId = ManagersVM.SelectedItem.VenueId,
                VenueName = ManagersVM.SelectedItem.VenueName
            };

            IsManagerFlyoutOpen = true;
        }

        /// <summary>
        /// Guarda el formulario de managers (creación o edición) usando el módulo CRUD.
        /// Si falla, mantiene el flyout abierto y conserva los datos introducidos.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        [RelayCommand]
        private async Task SaveManager()
        {
            if (ActiveManagerForm == null)
                return;

            bool ok;

            if (!IsEditingManager)
            {
                ok = await ManagersVM.TryAddAsync((CreateManagerDTO)ActiveManagerForm);
            }
            else
            {
                var dto = (ManagerDTO)ActiveManagerForm;
                ok = await ManagersVM.TryUpdateAsync(dto.Id, dto);
            }

            if (!ok)
            {
                // Mantener abierto y conservar datos para corregir errores
                IsManagerFlyoutOpen = true;
                return;
            }

            ActiveManagerForm = null;
            IsEditingManager = false;
            IsManagerFlyoutOpen = false;
        }

        /// <summary>
        /// Cierra el flyout de managers y limpia el estado del formulario.
        /// </summary>
        [RelayCommand]
        private void CloseManagerFlyout()
        {
            ActiveManagerForm = null;
            IsEditingManager = false;
            ManagersVM.ErrorMessage = null;

            IsManagerFlyoutOpen = false;
        }
    }
}
