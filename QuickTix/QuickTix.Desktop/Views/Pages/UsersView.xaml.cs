using QuickTix.Desktop.ViewModels.Users;

namespace QuickTix.Desktop.Views.Pages
{
    public partial class UsersView : INavigableView<UsersViewModel>
    {
        public UsersViewModel ViewModel { get; }
        public UsersView(UsersViewModel viewModel)
        {
            ViewModel = viewModel;


            InitializeComponent();

            DataContext = ViewModel;

            //viewModel.OnPageLoaded();
        }
    }
}
