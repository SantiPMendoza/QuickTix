using QuickTix.Core.Models.DTOs.SaleDTO;
using QuickTix.Core.Models.DTOs.SalesHistory;
using QuickTix.Core.Models.Entities;

namespace QuickTix.Core.Interfaces
{
    public interface ISaleRepository : IRepository<Sale> {

        Task<IEnumerable<TicketSaleDTO>> GetTicketHistoryAsync();
        Task<IEnumerable<SubscriptionSaleDTO>> GetSubscriptionHistoryAsync();

        Task<Sale> SellTicketsAsync(SellTicketDTO request);
        Task<Sale> SellSubscriptionAsync(SellSubscriptionDTO request);
    }
}
