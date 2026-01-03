using QuickTix.Mobile.ViewModels;

namespace QuickTix.Mobile.Views;

public partial class TicketsPage : ContentPage
{
    public TicketsPage(TicketsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is TicketsViewModel vm)
            await vm.InitializeAsync();
    }
}
