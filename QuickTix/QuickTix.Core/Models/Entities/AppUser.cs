using Microsoft.AspNetCore.Identity;

namespace QuickTix.Core.Models.Entities
{
    public class AppUser : IdentityUser
    {
        public string Name { get; set; } = null!;
        public string? Nif { get; set; }
        public bool MustChangePassword { get; set; } = false;
    }

}
