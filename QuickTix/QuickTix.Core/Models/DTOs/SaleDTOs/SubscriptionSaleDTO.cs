namespace QuickTix.Core.Models.DTOs.SalesHistory
{
    public class SubscriptionSaleDTO
    {
        public int Id { get; set; }                 // Id de Sale (o el que decidas)
        public DateTime Date { get; set; }

        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;

        public int ManagerId { get; set; }
        public string ManagerName { get; set; } = string.Empty;

        public string SubscriptionCategory { get; set; } = string.Empty;
        public decimal Price { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public string DiaSemanaString => Date.ToString("dddd");
    }
}
