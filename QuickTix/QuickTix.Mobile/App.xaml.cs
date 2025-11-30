using QuickTix.Mobile.Views;

namespace QuickTix.Mobile;

public partial class App : Application
{
    public App(LoginPage loginPage)
    {
        InitializeComponent();

        // Arranca directamente en LoginPage
        MainPage = loginPage;
    }
}
