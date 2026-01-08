using QuickTix.Desktop.ViewModels.Pricing;

namespace QuickTix.Desktop.Views.Pages
{
    public partial class PricingView : INavigableView<PricingViewModel>
    {
        public PricingViewModel ViewModel { get; }

        public PricingView(PricingViewModel viewModel)
        {
            ViewModel = viewModel;

            InitializeComponent();
            DataContext = ViewModel;

            Loaded += PricingView_Loaded;
        }

        private async void PricingView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Loaded -= PricingView_Loaded;

            await ViewModel.LoadVenuesAsync();
        }
    }
}
