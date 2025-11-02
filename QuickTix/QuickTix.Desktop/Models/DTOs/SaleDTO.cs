using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;

namespace QuickTix.Desktop.Models.DTOs
{
    public class SaleDTO : CreateSaleDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
    }

    public class CreateSaleDTO
    {
        [Required]
        public int VenueId { get; set; }

        [Required]
        public int ManagerId { get; set; }

        public int? TicketId { get; set; }
        public int? SubscriptionId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }
    }
}
