using QuickTix.Core.Enums;

namespace QuickTix.Core.Models.Entities
{
    public class Ticket
    {
        public int Id { get; set; }

        public int VenueId { get; set; }
        public Venue Venue { get; set; } = null!;

        public int? ClientId { get; set; }
        public Client? Client { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public decimal Price { get; set; }
        public TicketType Type { get; set; }
        public TicketContext Context { get; set; } = TicketContext.Normal;

        public Sale? Sale { get; set; } // Relación 1:1
    }
}
