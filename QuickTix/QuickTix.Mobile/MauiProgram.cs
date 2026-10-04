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

                // Tipografía del tema Vibra (handoff 1b): Space Grotesk para
                // titulares, DM Sans para cuerpo/UI, IBM Plex Mono para códigos.
                fonts.AddFont("SpaceGrotesk-SemiBold.ttf", "SpaceGrotesk");
                fonts.AddFont("SpaceGrotesk-Bold.ttf", "SpaceGroteskBold");
                fonts.AddFont("DMSans-Regular.ttf", "DMSans");
                fonts.AddFont("DMSans-Medium.ttf", "DMSansMedium");
                fonts.AddFont("DMSans-SemiBold.ttf", "DMSansSemiBold");
                fonts.AddFont("DMSans-Bold.ttf", "DMSansBold");
                fonts.AddFont("IBMPlexMono-Medium.ttf", "PlexMono");
                fonts.AddFont("IBMPlexMono-SemiBold.ttf", "PlexMonoSemiBold");
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
        builder.Services.AddTransient<ChangePasswordPage>();
        builder.Services.AddTransient<SubscriptionsPage>();



        return builder.Build();
    }
}
