namespace QuickTix.Desktop.Views.Pages
{
    /// <summary>
    /// Lógica de interacción para CashCloseView.xaml (cierre de caja, solo admin).
    /// </summary>
    public partial class CashCloseView : INavigableView<CashCloseViewModel>
    {
        public CashCloseViewModel ViewModel { get; }

        public CashCloseView(CashCloseViewModel viewModel)
        {
            ViewModel = viewModel;

            InitializeComponent();

            DataContext = ViewModel;

            // Cada vez que se abre la página se consulta el rango vigente (hoy por defecto):
            // el arqueo debe mostrar siempre datos frescos, sin esperar a pulsar «Consultar».
            // Solo aquí (navegación) se refresca el «hoy» por defecto; «Consultar» nunca toca las fechas.
            Loaded += (_, _) =>
            {
                if (ViewModel.IsAdmin)
                {
                    ViewModel.RefreshDefaultDates();
                    _ = ViewModel.LoadAsync();
                }
            };
        }
    }
}
