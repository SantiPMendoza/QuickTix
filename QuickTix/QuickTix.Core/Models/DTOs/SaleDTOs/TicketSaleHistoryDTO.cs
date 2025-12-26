namespace QuickTix.Core.Models.DTOs.SalesHistory
{
    public class TicketSaleHistoryDTO
    {
        public int Id { get; set; }                 // Id de Sale (o el que decidas)
        public DateTime Date { get; set; }

        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;

        public int ManagerId { get; set; }
        public string ManagerName { get; set; } = string.Empty;

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount => Quantity * UnitPrice;

        public string DiaSemanaString => Date.ToString("dddd");
    }
}
