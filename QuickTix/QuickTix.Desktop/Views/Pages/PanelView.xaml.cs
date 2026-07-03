namespace QuickTix.Desktop.Views.Pages
{
    /// <summary>
    /// Lógica de interacción para PanelView.xaml (dashboard, spec 3a).
    /// </summary>
    public partial class PanelView : INavigableView<PanelViewModel>
    {
        public PanelViewModel ViewModel { get; }

        public PanelView(PanelViewModel viewModel)
        {
            ViewModel = viewModel;

            InitializeComponent();

            DataContext = ViewModel;
        }
    }
}
