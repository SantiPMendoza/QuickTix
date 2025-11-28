using QuickTix.Mobile.Views;

namespace QuickTix.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        Routing.RegisterRoute("MainPage", typeof(MainPage));
    }
}
