using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickTix.Desktop.Models.UserDTO;
using System.Collections.ObjectModel;
using System.Windows;


namespace QuickTix.Desktop.ViewModels.Users
{
    public partial class UsersViewModel : ObservableObject
    {
        public AdminViewModel AdminsVM { get; }
        public ManagerViewModel ManagersVM { get; }

        public UsersViewModel(HttpJsonClient httpClient)
        {
            AdminsVM = new AdminViewModel(httpClient);
            ManagersVM = new ManagerViewModel(httpClient);
        }



    }


}

