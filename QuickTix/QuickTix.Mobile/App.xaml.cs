using QuickTix.Mobile.Views;

namespace QuickTix.Mobile;

public partial class App : Application
{
    public App(LoginPage loginPage)
    {
        InitializeComponent();


        MainPage = new NavigationPage(loginPage);
    }
}
