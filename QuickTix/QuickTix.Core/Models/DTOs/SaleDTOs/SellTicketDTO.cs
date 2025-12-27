using QuickTix.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Core.Models.DTOs.SaleDTO
{
    public class SellTicketDTO
    {
        [Required]
        public int VenueId { get; set; }

        [Required]
        public int ManagerId { get; set; }

        public int? ClientId { get; set; } // obligatorio si Context = InvitadoAbonado

        [Required]
        public TicketType Type { get; set; }

        [Required]
        public TicketContext Context { get; set; } = TicketContext.Normal;

        [Range(1, 1000)]
        public int Quantity { get; set; } = 1;

        [Range(0, double.MaxValue)]
        public decimal? UnitPrice { get; set; }
    }
}
