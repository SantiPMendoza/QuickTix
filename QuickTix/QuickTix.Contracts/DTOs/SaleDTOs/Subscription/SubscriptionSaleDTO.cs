namespace QuickTix.Contracts.DTOs.SaleDTOs.Subscription
{
    public class SubscriptionSaleDTO
    {
        public int Id { get; set; }
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
