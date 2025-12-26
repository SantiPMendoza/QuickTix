using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuickTix.Desktop.Services;
using QuickTix.Desktop.ViewModels.Sales;
using QuickTix.Desktop.ViewModels.Users;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;
using Wpf.Ui.DependencyInjection;

namespace QuickTix.Desktop
{
    /// <summary>
    /// Punto de entrada de la aplicación WPF QuickTix.
    /// Configura DI, HttpClient y servicios principales.
    /// </summary>
    public partial class App
    {
        private static readonly IHost _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(c =>
            {
                var basePath = Path.GetDirectoryName(AppContext.BaseDirectory)
                               ?? throw new DirectoryNotFoundException("Unable to find base directory.");
                _ = c.SetBasePath(basePath);
            })
            .ConfigureServices((context, services) =>
            {
                _ = services.AddNavigationViewPageProvider();

                // API Service
                services.AddSingleton(new HttpClient { BaseAddress = new Uri("https://localhost:7137/") });


                // App Host
                _ = services.AddHostedService<ApplicationHostService>();

                // Navigation service
                _ = services.AddSingleton<INavigationService, Wpf.Ui.NavigationService>();

                // Services
                services.AddSingleton<HttpJsonClient>();
                services.AddSingleton<IAuthService, AuthService>();




                // Main Window with Navigation
                _ = services.AddSingleton<INavigationWindow, Views.MainWindow>();
                _ = services.AddSingleton<MainViewModel>();
                


                // ViewModels
                _ = services.AddSingleton<LoginViewModel>();
                _ = services.AddSingleton<UsersViewModel>();
                _ = services.AddSingleton<TicketSalesViewModel>();
                _ = services.AddSingleton<SubscriptionsViewModel>();
                _ = services.AddSingleton<ClientsViewModel>();
                _ = services.AddSingleton<SalesViewModel>();
                _ = services.AddSingleton<SuscriptionSalesViewModel>();



                // Views
                _ = services.AddSingleton<LoginView>();
                _ = services.AddSingleton<UsersView>();
                _ = services.AddSingleton<SubscriptionsView>();
                _ = services.AddSingleton<ClientsView>();
                _ = services.AddSingleton<SalesView>();

                //_ = services.AddSingleton<Views.SplashScreen>();


                // Configuration
                //_ = services.Configure<Utils.AppConfig>(context.Configuration.GetSection(nameof(Utils.AppConfig)));
            })
            .Build();

        /// <summary>
        /// Gets the application's service provider.
        /// </summary>
        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// Occurs when the application starts.
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {

            await _host.StartAsync();
        }


        /// <summary>
        /// Occurs when the application exits.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }

        /// <summary>
        /// Handles unhandled exceptions.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
        }
    }
}

