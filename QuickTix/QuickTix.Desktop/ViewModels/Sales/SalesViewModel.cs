using QuickTix.Desktop.ViewModels.Base;

namespace QuickTix.Desktop.ViewModels.Sales
{
    public class SalesViewModel : ViewModel
    {
        public TicketSalesViewModel TicketSales { get; }
        public SuscriptionSalesViewModel SubscriptionSales { get; }

        public SalesViewModel(
            TicketSalesViewModel ticketSales,
            SuscriptionSalesViewModel subscriptionSales)
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
