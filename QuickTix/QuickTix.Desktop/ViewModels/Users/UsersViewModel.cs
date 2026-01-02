using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Desktop.ViewModels.Base;
using System.Collections.ObjectModel;

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

            IsAdminFlyoutOpen = true;
        }

        [RelayCommand]
        private async Task SaveAdmin()
        {
            if (!IsEditingAdmin)
            {
                await AdminsVM.AddAsync((CreateAdminDTO)ActiveAdminForm!);
            }
            else
            {
                var dto = (AdminDTO)ActiveAdminForm!;
                await AdminsVM.UpdateAsync(dto.Id, dto);
            }

            ActiveAdminForm = null;
            IsEditingAdmin = false;
            IsAdminFlyoutOpen = false;
        }

        [RelayCommand]
        private void CloseAdminFlyout() => IsAdminFlyoutOpen = false;



        // ============================================================
        // MANAGER
        // ============================================================

        [RelayCommand]
        private async Task OpenManagerFlyout()
        {
            IsEditingManager = false;
            ActiveManagerForm = new CreateManagerDTO();
            await ManagersVM.LoadVenuesAsync();
            IsManagerFlyoutOpen = true;
        }

        [RelayCommand]
        private async Task EditManager()
        {
            if (ManagersVM.SelectedItem == null)
                return;

            IsEditingManager = true;

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
            if (!IsEditingManager)
            {
                await ManagersVM.AddAsync((CreateManagerDTO)ActiveManagerForm!);
            }
            else
            {
                var dto = (ManagerDTO)ActiveManagerForm!;
                await ManagersVM.UpdateAsync(dto.Id, dto);
            }

            ActiveManagerForm = null;
            IsEditingManager = false;
            IsManagerFlyoutOpen = false;
        }

        [RelayCommand]
        private void CloseManagerFlyout() => IsManagerFlyoutOpen = false;
    }
}
