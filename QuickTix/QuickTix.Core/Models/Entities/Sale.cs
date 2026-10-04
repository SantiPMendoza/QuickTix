using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickTix.Contracts.Enums;

namespace QuickTix.Core.Models.Entities
{
    public class Sale
    {
        public int Id { get; set; }

        public int VenueId { get; set; }
        public Venue Venue { get; set; } = null!;

        // Null = venta registrada por administración (sin manager asociado).
        public int? ManagerId { get; set; }
        public Manager? Manager { get; set; }

        public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();

        public decimal TotalAmount => Items.Sum(i => i.UnitPrice * i.Quantity);

        public DateTime Date { get; set; } = DateTime.UtcNow;

        // Default Cash: las filas anteriores a S2 y los clientes que no envían medio de pago.
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        // Anulación lógica: la venta no se borra, queda fuera de arqueo y Panel.
        // VoidedAt en UTC, igual que Date.
        public DateTime? VoidedAt { get; set; }

        // Id del usuario (Identity) que anuló; string porque los ids de Identity son strings.
        public string? VoidedByUserId { get; set; }

        public string? VoidReason { get; set; }

        // Derivada de VoidedAt (no mapeada): una sola fuente de verdad.
        public bool IsVoided => VoidedAt != null;
    }
}
