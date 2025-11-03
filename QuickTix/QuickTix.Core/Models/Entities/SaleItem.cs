using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Core.Models.Entities
{
    public class SaleItem
    {
        public int Id { get; set; }

        public int SaleId { get; set; }
        public Sale Sale { get; set; } = null!;

        // 🔹 Campos opcionales según el tipo de producto
        public int? TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public int? SubscriptionId { get; set; }
        public Subscription? Subscription { get; set; }

        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }

        public decimal Subtotal => UnitPrice * Quantity;
    }
}
