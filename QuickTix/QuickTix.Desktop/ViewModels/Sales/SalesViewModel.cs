using QuickTix.Desktop.ViewModels.Base;

namespace QuickTix.Desktop.ViewModels.Sales
{
    public partial class SalesViewModel : ViewModel
    {
        public TicketSalesViewModel TicketSales { get; }
        public SubscriptionSalesViewModel SubscriptionSales { get; }

        public SalesViewModel(
            TicketSalesViewModel ticketSales,
            SubscriptionSalesViewModel subscriptionSales)
        {
            TicketSales = ticketSales;
            SubscriptionSales = subscriptionSales;

            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            await Task.WhenAll(
                TicketSales.LoadAsync(),
                SubscriptionSales.LoadAsync()
            );
        }
    }
}
