using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuickTix.Desktop.Services;
using System.Net.Http;
using System.Windows;

namespace QuickTix.Desktop
{
    /// <summary>
    /// Punto de entrada de la aplicación WPF QuickTix.
    /// Configura DI, HttpClient y servicios principales.
    /// </summary>
    public partial class App
    {
        private static readonly IHost _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Cliente HTTP
                services.AddSingleton(new HttpClient
                {
                    BaseAddress = new Uri("https://localhost:7280/") // Cambiar al dominio real en producción
                });

                // Servicios
                services.AddSingleton<IAuthService, AuthService>();
                services.AddSingleton<HttpJsonClient>();

                // Ventanas y ViewModels
                services.AddSingleton<MainWindow>();
            })
            .Build();

        public static IServiceProvider Services => _host.Services;

        private async void OnStartup(object sender, StartupEventArgs e)
        {
            await _host.StartAsync();
            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
