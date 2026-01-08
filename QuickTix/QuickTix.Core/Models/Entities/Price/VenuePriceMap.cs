using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Core.Models.Entities.Price
{
    public class VenuePriceMap
    {
        public int VenueId { get; set; }
        public List<VenueTicketPrice> TicketPrices { get; set; } = new();
        public List<VenueSubscriptionPrice> SubscriptionPrices { get; set; } = new();
    }
}
