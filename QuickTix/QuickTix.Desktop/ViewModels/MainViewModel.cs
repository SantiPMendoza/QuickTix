
using QuickTix.Desktop.ViewModels.Base;
using Wpf.Ui.Controls;

namespace QuickTix.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel principal de la aplicación Desktop.
    /// Configura los elementos de navegación (menú y footer) y controla su visibilidad inicial.
    /// </summary>
    public partial class MainViewModel : ViewModel
    {
        private readonly INavigationService _navigationService;
        private bool _isInitialized = false;

        [ObservableProperty] private string applicationTitle = "QuickTix";
        [ObservableProperty] private ObservableCollection<object> navigationItems = [];
        [ObservableProperty] private ObservableCollection<object> navigationFooter = [];
        [ObservableProperty] private Visibility navigationVisibility = Visibility.Hidden;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="MainViewModel"/>.
        /// Configura el menú de navegación y aplica un retraso de visibilidad para suavizar la carga inicial.
        /// </summary>
        /// <param name="navigationService">Servicio de navegación proporcionado por Wpf.Ui.</param>
        public MainViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            if (!_isInitialized)
            {
                InitializeViewModel();
                _ = ShowNavigationAfterDelay();
            }
        }

        /// <summary>
        /// Inicializa los elementos del menú de navegación y del pie de navegación.
        /// </summary>
        /// <remarks>
        /// Los items se definen como <see cref="NavigationViewItem"/> y apuntan a páginas (TargetPageType).
        /// Los iconos se asignan aquí como SymbolIcon directos (ruta nativa de WPF-UI):
        /// asignar el glifo por triggers de plantilla no funciona porque SymbolIcon
        /// construye su glifo de forma perezosa. Mapeo del handoff Vibra:
        /// Panel=grid, Usuarios=personas, Ventas=recibo, Precios=etiqueta, Clientes=persona.
        /// </remarks>
        private void InitializeViewModel()
        {
            NavigationItems =
            [
                // Panel (dashboard, spec 3a): primer ítem y vista inicial tras login
                new NavigationViewItem
                {
                    Content = "Panel",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Grid24 },
                    TargetPageType = typeof(PanelView)
                },
                new NavigationViewItem
                {
                    Content = "Usuarios",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.People24 },
                    TargetPageType = typeof(UsersView)
                },
                new NavigationViewItem
                {
                    Content = "Historial de\nventas",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Receipt24 },
                    TargetPageType = typeof(SalesView)
                },
                new NavigationViewItem
                {
                    Content = "Precios",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Tag24 },
                    TargetPageType = typeof(PricingView)
                },
                new NavigationViewItem
                {
                    Content = "Clientes",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Person24 },
                    TargetPageType = typeof(ClientsView)
                },
            ];

            NavigationFooter =
            [
                new NavigationViewItem
                {
                    Content = "Logout",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowExit20 },
                    TargetPageType = typeof(LoginView)
                },
            ];

            _isInitialized = true;
        }

        /// <summary>
        /// Hace visible el menú de navegación tras un pequeño retraso para mejorar la percepción visual de carga.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        public async Task ShowNavigationAfterDelay()
        {
            await Task.Delay(750);
            NavigationVisibility = Visibility.Visible;
        }
    }
}
