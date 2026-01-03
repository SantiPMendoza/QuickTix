using QuickTix.Mobile.Views;

namespace QuickTix.Mobile;

public partial class AppShell_Manager : Shell
{
    public AppShell_Manager()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(TicketsPage), typeof(TicketsPage));
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
    }
}
