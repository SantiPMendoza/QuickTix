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

        public int? TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public int? SubscriptionId { get; set; }
        public Subscription? Subscription { get; set; }

        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
