
namespace QuickTix.Desktop.Views.Pages
{
    /// <summary>
    /// Lógica de interacción para LoginPage.xaml
    /// </summary>
    public partial class LoginView : INavigableView<LoginViewModel>
    {
        public LoginViewModel ViewModel { get; }
        public LoginView(LoginViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
