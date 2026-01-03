using QuickTix.Contracts.Enums;

namespace QuickTix.Contracts.DTOs.SaleDTOs.Ticket
{
    public class TicketSaleDetailDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }

        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;

        public int ManagerId { get; set; }
        public string ManagerName { get; set; } = string.Empty;

        public string? InvitedByClientName { get; set; }


        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }

        public List<TicketSaleDetailLineDTO> Lines { get; set; } = new();

        public string DiaSemanaString => Date.ToString("dddd");
    }

    public class TicketSaleDetailLineDTO
    {
        public TicketType Type { get; set; }
        public TicketContext Context { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
