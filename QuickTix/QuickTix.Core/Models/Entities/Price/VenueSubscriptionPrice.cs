using QuickTix.Contracts.Enums;

namespace QuickTix.Core.Models.Entities.Price
{
    public class VenueSubscriptionPrice
    {
        public int Id { get; set; }

        public int VenueId { get; set; }
        public Venue Venue { get; set; } = null!;

        public SubscriptionCategory Category { get; set; }
        public SubscriptionDuration Duration { get; set; }

        public decimal Price { get; set; }
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
