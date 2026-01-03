namespace QuickTix.Contracts.DTOs.SaleDTOs.Ticket
{
    public class TicketSaleDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }

        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;

        public int ManagerId { get; set; }
        public string ManagerName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        // Total real de la venta (suma de líneas)
        public decimal TotalAmount { get; set; }

        public string DiaSemanaString => Date.ToString("dddd");
    }
}
