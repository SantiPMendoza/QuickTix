using QuickTix.Contracts.Enums;

namespace QuickTix.Contracts.DTOs.SaleDTOs.Subscription
{
    public class SubscriptionSaleDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }

        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;

        // Null = venta registrada por administración (sin manager asociado).
        public int? ManagerId { get; set; }
        public string ManagerName { get; set; } = string.Empty;

        public string SubscriptionCategory { get; set; } = string.Empty;
        public decimal Price { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public PaymentMethod PaymentMethod { get; set; }

        // Anulación lógica: la venta sigue en el historial pero marcada.
        public bool IsVoided { get; set; }
        public DateTime? VoidedAt { get; set; }
        public string? VoidReason { get; set; }

        public string DiaSemanaString => Date.ToString("dddd");
    }
}
