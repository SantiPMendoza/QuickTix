using QuickTix.Desktop.ViewModels.Base;

namespace QuickTix.Desktop.ViewModels.Sales
{
    /// <summary>
    /// ViewModel raíz de la sección de ventas.
    /// Orquesta la carga y coordinación de los historiales de ventas
    /// de tickets y suscripciones.
    /// </summary>
    public partial class SalesViewModel : ViewModel
    {
        /// <summary>
        /// ViewModel del histórico de ventas de tickets.
        /// </summary>
        public TicketSalesViewModel TicketSales { get; }

        /// <summary>
        /// ViewModel del histórico de ventas de suscripciones.
        /// </summary>
        public SubscriptionSalesViewModel SubscriptionSales { get; }

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="SalesViewModel"/>.
        /// </summary>
        /// <param name="ticketSales">ViewModel de ventas de tickets.</param>
        /// <param name="subscriptionSales">ViewModel de ventas de suscripciones.</param>
        public SalesViewModel(
            TicketSalesViewModel ticketSales,
            SubscriptionSalesViewModel subscriptionSales)
        {
            TicketSales = ticketSales;
            SubscriptionSales = subscriptionSales;

            _ = LoadAsync();
        }

        /// <summary>
        /// Carga ambos historiales de ventas en paralelo.
        /// </summary>
        /// <returns>Tarea asíncrona.</returns>
        public Task LoadAsync()
            => Task.WhenAll(
                TicketSales.LoadAsync(),
                SubscriptionSales.LoadAsync()
            );
    }
}
