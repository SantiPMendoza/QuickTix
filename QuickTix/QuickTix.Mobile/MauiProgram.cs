using QuickTix.Mobile.Views;
using QuickTix.Mobile.ViewModels;
using QuickTix.Mobile.Services;
using QuickTix.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Servicios
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<HttpJsonClient>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        //builder.Services.AddTransient<MainViewModel>(); // si tienes uno

        // Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
