using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace QuickTix.Core.Models.Entities
{
    public class Client
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public string AppUserId { get; set; } = null!;
        public AppUser AppUser { get; set; } = null!;

        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>(); // opcional
    }
}


