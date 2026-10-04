
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
        private readonly IAuthService _authService;
        private bool _isInitialized = false;

        // Ítem «Cierre de caja»: solo lo ve el admin (la API responde 403 al resto)
        private NavigationViewItem? _cashCloseItem;

        [ObservableProperty] private string applicationTitle = "QuickTix";
        [ObservableProperty] private ObservableCollection<object> navigationItems = [];
        [ObservableProperty] private ObservableCollection<object> navigationFooter = [];
        [ObservableProperty] private Visibility navigationVisibility = Visibility.Hidden;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="MainViewModel"/>.
        /// Configura el menú de navegación y aplica un retraso de visibilidad para suavizar la carga inicial.
        /// </summary>
        /// <param name="navigationService">Servicio de navegación proporcionado por Wpf.Ui.</param>
        /// <param name="authService">Servicio de autenticación (rol de la sesión).</param>
        public MainViewModel(INavigationService navigationService, IAuthService authService)
        {
            _navigationService = navigationService;
            _authService = authService;

            // El menú se construye antes del login: se reevalúa al iniciar/cerrar sesión
            _authService.SessionChanged += UpdateRoleVisibility;

            if (!_isInitialized)
            {
                InitializeViewModel();

                // Si el VM se construye con una sesión admin ya iniciada, SessionChanged no volverá a
                // dispararse: hay que aplicar la visibilidad por rol ahora.
                UpdateRoleVisibility();

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
            // Arqueo: admin only. Oculto hasta que una sesión admin lo muestre (UpdateRoleVisibility).
            _cashCloseItem = new NavigationViewItem
            {
                Content = "Cierre de caja",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Calculator24 },
                TargetPageType = typeof(CashCloseView),
                Visibility = Visibility.Collapsed
            };

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
                _cashCloseItem,
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

            // Navegar a LoginView por sí solo no cerraba la sesión (token y rol seguían vivos y
            // SessionChanged no se disparaba): el clic llama primero a IAuthService.Logout().
            var logoutItem = new NavigationViewItem
            {
                Content = "Logout",
                Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowExit20 },
                TargetPageType = typeof(LoginView)
            };
            logoutItem.Click += (_, _) => _authService.Logout();

            NavigationFooter = [logoutItem];

            _isInitialized = true;
        }

        /// <summary>
        /// Muestra u oculta los ítems de menú que dependen del rol de la sesión actual.
        /// </summary>
        private void UpdateRoleVisibility()
        {
            if (_cashCloseItem != null)
                _cashCloseItem.Visibility = _authService.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
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
