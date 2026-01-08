using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Desktop.Services;
using QuickTix.Desktop.ViewModels.Base;

namespace QuickTix.Desktop.ViewModels.Users
{
    public partial class UsersViewModel : ViewModel
    {
        public AdminViewModel AdminsVM { get; }
        public ManagerViewModel ManagersVM { get; }

        // Flyouts
        [ObservableProperty] private bool isAdminFlyoutOpen;
        [ObservableProperty] private bool isManagerFlyoutOpen;

        // Estados de edición
        [ObservableProperty] private bool isEditingAdmin;
        [ObservableProperty] private bool isEditingManager;

        // Formularios activos (crear o editar)
        [ObservableProperty] private object? activeAdminForm;
        [ObservableProperty] private object? activeManagerForm;

        public UsersViewModel(HttpJsonClient httpClient)
        {
            AdminsVM = new AdminViewModel(httpClient);
            ManagersVM = new ManagerViewModel(httpClient);
        }

        // ============================================================
        // ADMIN
        // ============================================================

        [RelayCommand]
        private void OpenAdminFlyout()
        {
            IsEditingAdmin = false;
            ActiveAdminForm = new CreateAdminDTO();

            // Limpia errores previos para no arrastrarlos
            AdminsVM.ErrorMessage = null;

            IsAdminFlyoutOpen = true;
        }

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
                // Mantener abierto, mantener datos
                IsAdminFlyoutOpen = true;
                return;
            }

            // Solo si éxito: limpiar y cerrar
            ActiveAdminForm = null;
            IsEditingAdmin = false;
            IsAdminFlyoutOpen = false;
        }


        [RelayCommand]
        private void CloseAdminFlyout()
        {
            // Cancelar: cerrar y limpiar explícitamente
            ActiveAdminForm = null;
            IsEditingAdmin = false;
            AdminsVM.ErrorMessage = null;

            IsAdminFlyoutOpen = false;
        }

        // ============================================================
        // MANAGER
        // ============================================================

        [RelayCommand]
        private async Task OpenManagerFlyout()
        {
            IsEditingManager = false;
            ActiveManagerForm = new CreateManagerDTO();

            ManagersVM.ErrorMessage = null;

            await ManagersVM.LoadVenuesAsync();
            IsManagerFlyoutOpen = true;
        }

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
                IsManagerFlyoutOpen = true;
                return;
            }

            ActiveManagerForm = null;
            IsEditingManager = false;
            IsManagerFlyoutOpen = false;
        }

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
