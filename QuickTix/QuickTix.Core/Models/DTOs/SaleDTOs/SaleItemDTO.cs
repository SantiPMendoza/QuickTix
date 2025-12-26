using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Core.Models.DTOs.SaleDTOs
{
    public class SaleItemDTO
    {
        public int Id { get; set; }

        public int? TicketId { get; set; }
        public int? SubscriptionId { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal Subtotal => UnitPrice * Quantity;
    }

    // ============================
    // 🔹 CreateSaleItemDTO (creación)
    // ============================
    public class CreateSaleItemDTO
    {
        public int? TicketId { get; set; }
        public int? SubscriptionId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que cero.")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El precio debe ser un valor positivo.")]
        public decimal UnitPrice { get; set; }
    }
}

