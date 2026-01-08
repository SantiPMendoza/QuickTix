using QuickTix.Core.Models.Entities;

namespace QuickTix.Core.Interfaces
{
    public interface ISaleItemRepository
    {
        Task<ICollection<SaleItem>> GetAllAsync();
        Task<SaleItem?> GetAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<ICollection<SaleItem>> GetTicketsAsync();
        Task<ICollection<SaleItem>> GetSubscriptionsAsync();
        Task<ICollection<SaleItem>> GetBySaleAsync(int saleId);
    }
}
