using QuickTix.Mobile.Views;

namespace QuickTix.Mobile;

public partial class App : Application
{
    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        // LoginPage se resuelve DESPUÉS de InitializeComponent: si se inyecta por
        // constructor, la página se infla antes de que existan los recursos de App
        // y cualquier StaticResource del tema lanza XamlParseException.
        MainPage = new NavigationPage(serviceProvider.GetRequiredService<LoginPage>());
    }
}
