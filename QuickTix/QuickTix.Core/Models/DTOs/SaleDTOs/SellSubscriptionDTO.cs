using QuickTix.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Core.Models.DTOs.SaleDTO
{
    public class SellSubscriptionDTO
    {
        [Required]
        public int VenueId { get; set; }

        [Required]
        public int ManagerId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        public SubscriptionCategory Category { get; set; }

        [Required]
        public SubscriptionDuration Duration { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        // La UI puede mandar 0 y que backend lo calcule.
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; } = 0m;
    }
}
