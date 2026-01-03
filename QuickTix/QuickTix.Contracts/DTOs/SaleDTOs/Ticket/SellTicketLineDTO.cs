using QuickTix.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Contracts.DTOs.SaleDTOs.Ticket
{

    public class SellTicketLineDTO
    {
        [Required]
        public TicketType Type { get; set; }

        [Required]
        public TicketContext Context { get; set; } = TicketContext.Normal;

        [Range(1, 1000)]
        public int Quantity { get; set; } = 1;

        [Range(0, double.MaxValue, ErrorMessage = "El precio debe ser positivo.")]
        public decimal? UnitPrice { get; set; }
    }
}
