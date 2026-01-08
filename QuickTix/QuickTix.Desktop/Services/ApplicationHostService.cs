using Wpf.Ui;

namespace QuickTix.Desktop.Services
{
    /// <summary>
    /// Servicio de hosting responsable de activar la aplicación WPF y mostrar la ventana principal
    /// cuando el host está listo. Centraliza la navegación inicial.
    /// </summary>
    public class ApplicationHostService(IServiceProvider serviceProvider) : IHostedService
    {
        // Ventana de navegación (Wpf.Ui) usada para mostrar la UI y navegar entre vistas
        private INavigationWindow? _navigationWindow;

        /// <summary>
        /// Se ejecuta cuando el host de la aplicación está listo para iniciar el servicio.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación del arranque.</param>
        /// <returns>Tarea asíncrona.</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await HandleActivationAsync();
        }

        /// <summary>
        /// Se ejecuta cuando el host realiza un apagado controlado.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación del apagado.</param>
        /// <returns>Tarea completada.</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Gestiona la activación inicial de la aplicación:
        /// crea/obtiene la ventana de navegación, la muestra y navega a la vista inicial (Login).
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        private async Task HandleActivationAsync()
        {
            await Task.CompletedTask;

            // Evita instanciar otra MainWindow si ya existe una en el árbol de ventanas
            if (!Application.Current.Windows.OfType<Views.MainWindow>().Any())
            {
                // Obtiene la ventana de navegación desde el contenedor de DI
                _navigationWindow = (serviceProvider.GetService(typeof(INavigationWindow)) as INavigationWindow)!;

                // Muestra la ventana y navega a la vista de login
                _navigationWindow!.ShowWindow();
                _ = _navigationWindow.Navigate(typeof(LoginView));
            }

            await Task.CompletedTask;
        }
    }
}
