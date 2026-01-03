using QuickTix.Mobile.Views;
using QuickTix.Mobile.ViewModels;
using QuickTix.Mobile.Services;
using QuickTix.Mobile;
using QuickTix.Mobile.Helpers;

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
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<HttpJsonClient>();
        builder.Services.AddSingleton<TokenStore>();
        builder.Services.AddSingleton<ITokenStore>(sp => sp.GetRequiredService<TokenStore>());
        builder.Services.AddSingleton<IAppSession, AppSession>();

        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7137/")
        });

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<ChangePasswordViewModel>();
        builder.Services.AddTransient<TicketsViewModel>();

        // Pages
        builder.Services.AddTransient<TicketsPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ChangePasswordPage>();



        return builder.Build();
    }
}
