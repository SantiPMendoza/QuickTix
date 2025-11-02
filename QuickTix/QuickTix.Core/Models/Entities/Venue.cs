namespace QuickTix.Core.Models.Entities
{
    public class Venue
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Location { get; set; } = null!;
        public int Capacity { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Manager> Managers { get; set; } = new List<Manager>();
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
