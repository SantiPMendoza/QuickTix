using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Core.Models.Entities
{
    public class Sale
    {
        public int Id { get; set; }

        public int VenueId { get; set; }
        public Venue Venue { get; set; } = null!;

        public int ManagerId { get; set; }
        public Manager Manager { get; set; } = null!;

        public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();

        public decimal TotalAmount => Items.Sum(i => i.UnitPrice * i.Quantity);

        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
