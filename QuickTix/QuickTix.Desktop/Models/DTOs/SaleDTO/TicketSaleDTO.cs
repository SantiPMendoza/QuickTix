using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Desktop.Models.DTOs.SaleDTO
{
    public class TicketSaleDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }

        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;

        public int ManagerId { get; set; }
        public string ManagerName { get; set; } = string.Empty;

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount => Quantity * UnitPrice;

        public string DiaSemanaString => Date.ToString("dddd");
    }

}
