using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Core.Models.Entities
{
    public class Manager
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public string AppUserId { get; set; } = null!;
        public AppUser AppUser { get; set; } = null!;

        public int VenueId { get; set; }
        public Venue Venue { get; set; } = null!;

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
