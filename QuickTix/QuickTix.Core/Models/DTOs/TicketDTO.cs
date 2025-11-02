using QuickTix.Core.Enums;
using QuickTix.Core.Models.Entities;
using System;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Core.Models.DTOs
{
    public class TicketDTO : CreateTicketDTO
    {
        public int Id { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string VenueName { get; set; } = null!;
    }

    public class CreateTicketDTO
    {
        [Required]
        public int VenueId { get; set; }

        public int? ClientId { get; set; } // opcional, si es invitado

        [Required]
        public TicketType Type { get; set; }

        [Required]
        public TicketContext Context { get; set; } = TicketContext.Normal;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
    }
}

