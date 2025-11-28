namespace QuickTix.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Hace que al arrancar la app muestre LoginPage
        MainPage = new AppShell();
        Shell.Current.GoToAsync("//LoginPage");
    }
}
