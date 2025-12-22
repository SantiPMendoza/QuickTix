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
    public class SubscriptionDTO : CreateSubscriptionDTO
    {
        public int Id { get; set; }
       
        public DateTime EndDate { get; set; }

        public string Status => DateTime.UtcNow.Date <= EndDate.Date ? "Activo" : "Caducado";

    }

    public class CreateSubscriptionDTO
    {
        [Required]
        public int VenueId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public SubscriptionCategory Category { get; set; }

        [Required]
        public SubscriptionDuration Duration { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
    }
}
