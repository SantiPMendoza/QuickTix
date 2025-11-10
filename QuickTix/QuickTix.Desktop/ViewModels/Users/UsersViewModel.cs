using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTix.Desktop.Models.DTOs;
using QuickTix.Desktop.Models.UserDTO;
using System.Collections.ObjectModel;
using System.Windows;


namespace QuickTix.Desktop.ViewModels.Users
{
    public partial class UsersViewModel : ObservableObject
    {

        [ObservableProperty] private bool isAdminFlyoutOpen;
        [ObservableProperty] private bool isManagerFlyoutOpen;

        [ObservableProperty] private CreateAdminDTO newAdmin = new();
        [ObservableProperty] private CreateManagerDTO newManager = new();

        [RelayCommand] private void OpenAdminFlyout() => IsAdminFlyoutOpen = true;
        [RelayCommand] private void CloseAdminFlyout() => IsAdminFlyoutOpen = false;

        [RelayCommand] private void OpenManagerFlyout() => IsManagerFlyoutOpen = true;
        [RelayCommand] private void CloseManagerFlyout() => IsManagerFlyoutOpen = false;


        public AdminViewModel AdminsVM { get; }
        public ManagerViewModel ManagersVM { get; }

        public UsersViewModel(HttpJsonClient httpClient)
        {
            AdminsVM = new AdminViewModel(httpClient);
            ManagersVM = new ManagerViewModel(httpClient);
        }


        [RelayCommand]
        private async Task SaveAdmin()
        {
            await AdminsVM.AddAsync(NewAdmin);
            NewAdmin = new CreateAdminDTO();
            IsAdminFlyoutOpen = false;
        }

        [RelayCommand]
        private async Task SaveManager()
        {
            await ManagersVM.AddAsync(NewManager);
            NewManager = new CreateManagerDTO();
            IsManagerFlyoutOpen = false;
        }
    }


}

