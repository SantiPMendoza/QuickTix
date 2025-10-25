using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuickTix.Core.Models.Entities;

namespace QuickTix.API.Data
{
    public class QuickTixDbContext : IdentityDbContext<AppUser>
    {
        public QuickTixDbContext(DbContextOptions<QuickTixDbContext> options) : base(options)
        {
        }

        // Aquí añadirás tus entidades normales (Pool, Ticket, etc.)
        // public DbSet<Ticket> Tickets { get; set; }
    }
}
