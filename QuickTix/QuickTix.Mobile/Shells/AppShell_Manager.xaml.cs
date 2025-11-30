using QuickTix.Mobile.Views;

namespace QuickTix.Mobile;

public partial class AppShell_Manager : Shell
{
    public AppShell_Manager()
    {
        InitializeComponent();

        Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        Routing.RegisterRoute("MainPage", typeof(MainPage));
    }
}
