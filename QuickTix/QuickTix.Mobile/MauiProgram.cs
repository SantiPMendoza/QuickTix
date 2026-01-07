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
        builder.Services.AddSingleton<ISubscriptionService, SubscriptionService>();


        builder.Services.AddSingleton(sp => new HttpClient
        {
            BaseAddress = new Uri(ApiConfig.GetBaseUrl().TrimEnd('/') + "/")
        });

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<ChangePasswordViewModel>();
        builder.Services.AddTransient<TicketsViewModel>();
        builder.Services.AddTransient<SubscriptionsViewModel>();

        // Pages
        builder.Services.AddTransient<TicketsPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ChangePasswordPage>();
        builder.Services.AddTransient<SubscriptionsPage>();



        return builder.Build();
    }
}
