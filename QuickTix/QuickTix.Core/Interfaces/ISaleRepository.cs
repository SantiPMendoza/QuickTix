using QuickTix.Contracts.DTOs.SaleDTOs.Subscription;
using QuickTix.Contracts.DTOs.SaleDTOs.Ticket;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Core.Models.Entities;

namespace QuickTix.Core.Interfaces
{
    public interface ISaleRepository : IRepository<Sale> {

        Task<IEnumerable<TicketSaleDTO>> GetTicketHistoryAsync();
        Task<IEnumerable<SubscriptionSaleDTO>> GetSubscriptionHistoryAsync();

        Task<Sale> SellTicketsAsync(SellTicketDTO request);
        Task<Sale> SellSubscriptionAsync(SellSubscriptionDTO request);

        Task<TicketSaleDetailDTO> GetTicketHistoryDetailAsync(int saleId);

        Task<Sale> SellTicketsBatchAsync(SellTicketsBatchDTO request);


    }
}
