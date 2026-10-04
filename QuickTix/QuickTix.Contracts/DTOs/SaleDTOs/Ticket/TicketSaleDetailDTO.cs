using QuickTix.Contracts.Enums;

namespace QuickTix.Contracts.DTOs.SaleDTOs.Ticket
{
    public class TicketSaleDetailDTO
    {
        public int Id { get; set; }

        // Fecha/hora LOCAL de Madrid (DateTimeKind.Unspecified), ya convertida desde el UTC almacenado:
        // los clientes la muestran tal cual, sin ToLocalTime().
        public DateTime Date { get; set; }

        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;

        public int ManagerId { get; set; }
        public string ManagerName { get; set; } = string.Empty;

        public string? InvitedByClientName { get; set; }


        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        // Anulación lógica: la venta sigue en el historial pero marcada.
        public bool IsVoided { get; set; }
        // Hora local de Madrid (Unspecified), como Date.
        public DateTime? VoidedAt { get; set; }
        public string? VoidReason { get; set; }

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
