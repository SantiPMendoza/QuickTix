

using QuickTix.Contracts.Enums;

namespace QuickTix.Core.Models.Entities
{
    public class Subscription
    {
        public int Id { get; set; }

        public int VenueId { get; set; }
        public Venue Venue { get; set; } = null!;

        public int ClientId { get; set; }
        public Client Client { get; set; } = null!;

        public SubscriptionCategory Category { get; set; }
        public SubscriptionDuration Duration { get; set; }

        public decimal Price { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public Sale? Sale { get; set; }
    }
}
