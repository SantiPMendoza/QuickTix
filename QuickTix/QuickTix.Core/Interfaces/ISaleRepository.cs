using QuickTix.Core.Models.DTOs.SalesHistory;
using QuickTix.Core.Models.Entities;

namespace QuickTix.Core.Interfaces
{
    public interface ISaleRepository : IRepository<Sale> {

        Task<IEnumerable<TicketSaleHistoryDTO>> GetTicketHistoryAsync();
        Task<IEnumerable<SubscriptionSaleHistoryDTO>> GetSubscriptionHistoryAsync();
    }
}
