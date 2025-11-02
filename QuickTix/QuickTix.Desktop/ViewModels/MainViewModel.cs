using QuickTix.Desktop.ViewModels.Base;
using Wpf.Ui.Controls;

namespace QuickTix.Desktop.ViewModels
{
    public partial class MainViewModel : ViewModel
    {
        private readonly INavigationService _navigationService;
        private bool _isInitialized = false;

        // Variables de navegación
        [ObservableProperty]
        private string _applicationTitle = "QuickTix";

        [ObservableProperty]
        private ObservableCollection<object> _navigationItems = [];

        [ObservableProperty]
        private ObservableCollection<object> _navigationFooter = [];

        [ObservableProperty]
        private Visibility navigationVisibility = Visibility.Hidden;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="MainViewModel"/>.
        /// </summary>
        /// <param name="navigationService">Servicio de navegación.</param>
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
        /// Inicializa los elementos de navegación y pie de navegación.
        /// </summary>
        private void InitializeViewModel()
        {
            NavigationItems =
            [
                new NavigationViewItem()
                {
                    Content = "Reservas",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Add16 },
                    //TargetPageType = typeof(ReservasView)
                },
                new NavigationViewItem()
                {
                    Content = "Calendario?",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Album20 },
                    //TargetPageType = typeof(CalendarView)
                },
                new NavigationViewItem()
                {
                    Content = "Configuración de\ndatos",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Clock12 },
                    //TargetPageType = typeof(ConfigView)
                },
            ];

            NavigationFooter =
            [
                new NavigationViewItem()
                {
                    Content = "Logout",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowExit20 },
                    //TargetPageType = typeof(ConfigView)
                },
            ];

            _isInitialized = true;
        }

        /// <summary>
        /// Muestra la navegación tras un retraso para mejorar la experiencia visual.
        /// </summary>
        /// <returns>Una tarea que representa la operación asincrónica.</returns>
        public async Task ShowNavigationAfterDelay()
        {
            await Task.Delay(750);
            NavigationVisibility = Visibility.Visible;
        }
    }
}
