using QuickTix.Contracts.Enums;

namespace QuickTix.Contracts.Models.DTOs.Pricing
{
    public class VenueTicketPriceDTO
    {
        public int VenueId { get; set; }
        public TicketType Type { get; set; }
        public TicketContext Context { get; set; }
        public decimal Price { get; set; }
    }

    public class VenueSubscriptionPriceDTO
    {
        public int VenueId { get; set; }
        public SubscriptionCategory Category { get; set; }
        public SubscriptionDuration Duration { get; set; }
        public decimal Price { get; set; }
    }

    public class VenuePriceMapDTO
    {
        public int VenueId { get; set; }
        public string? VenueName { get; set; }
        public List<VenueTicketPriceDTO> TicketPrices { get; set; } = new();
        public List<VenueSubscriptionPriceDTO> SubscriptionPrices { get; set; } = new();
    }


    public class UpsertVenuePriceMapDTO
    {
        public int VenueId { get; set; }
        public List<VenueTicketPriceDTO> TicketPrices { get; set; } = new();
        public List<VenueSubscriptionPriceDTO> SubscriptionPrices { get; set; } = new();
    }
}
