using QuickTix.Contracts.Enums;

namespace QuickTix.Core.Models.Entities.Price
{
    public class VenueTicketPrice
    {
        public int Id { get; set; }

        public int VenueId { get; set; }
        public Venue Venue { get; set; } = null!;

        public TicketType Type { get; set; }
        public TicketContext Context { get; set; }

        public decimal Price { get; set; }
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
