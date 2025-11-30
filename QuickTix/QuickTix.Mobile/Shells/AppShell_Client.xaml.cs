using QuickTix.Mobile.Views;

namespace QuickTix.Mobile;

public partial class AppShell_Client : Shell
{
    public AppShell_Client()
    {
        InitializeComponent();

        Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        Routing.RegisterRoute("MainPage", typeof(MainPage));
    }
}
