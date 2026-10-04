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

        /// <summary>
        /// Anula lógicamente una venta (nunca se borra). Valida el motivo (recortado, obligatorio, máx. 200).
        /// No toca Ticket/Subscription asociados. Invalida la caché de ventas.
        /// </summary>
        /// <param name="saleId">Venta a anular.</param>
        /// <param name="userId">Id (Identity) del usuario que anula.</param>
        /// <param name="reason">Motivo de la anulación.</param>
        /// <param name="nowUtc">Instante UTC de la anulación.</param>
        Task<VoidSaleResult> VoidAsync(int saleId, string userId, string reason, DateTime nowUtc);


    }
}
