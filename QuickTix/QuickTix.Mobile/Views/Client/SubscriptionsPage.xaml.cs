using QuickTix.Mobile.ViewModels;

namespace QuickTix.Mobile.Views;

public partial class SubscriptionsPage : ContentPage
{
    private readonly SubscriptionsViewModel _vm;

    public SubscriptionsPage(SubscriptionsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
