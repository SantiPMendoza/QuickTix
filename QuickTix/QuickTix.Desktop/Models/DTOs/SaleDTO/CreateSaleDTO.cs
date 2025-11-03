using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Desktop.Models.DTOs.SaleDTO
{
    public class CreateSaleDTO
    {
        [Required]
        public int VenueId { get; set; }

        [Required]
        public int ManagerId { get; set; }

        // Si es venta de tickets
        public int[]? TicketId { get; set; }

        // Si es venta de suscripción
        public int? SubscriptionId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }
    }

}
