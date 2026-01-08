using QuickTix.Core.Models.Entities.Price;

namespace QuickTix.Core.Interfaces
{
    public interface IPricingRepository
    {
        Task<VenuePriceMap> GetVenuePriceMapAsync(int venueId);
        Task<VenuePriceMap> UpsertVenuePriceMapAsync(VenuePriceMap map);
        void ClearCache();
    }

}
