namespace QuickTix.Core.Models.Entities
{
    public class Admin
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public string AppUserId { get; set; } = null!; // FK a Identity
        public AppUser AppUser { get; set; } = null!;
    }


}
