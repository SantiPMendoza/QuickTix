using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuickTix.Core.Models.Entities;

namespace QuickTix.DAL.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Tablas principales
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Manager> Managers { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Sale> Sales { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Admin ↔ AppUser (1:1)
            modelBuilder.Entity<Admin>()
                .HasOne(a => a.AppUser)
                .WithOne()
                .HasForeignKey<Admin>(a => a.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Manager ↔ AppUser (1:1)
            modelBuilder.Entity<Manager>()
                .HasOne(m => m.AppUser)
                .WithOne()
                .HasForeignKey<Manager>(m => m.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Client ↔ AppUser (1:1)
            modelBuilder.Entity<Client>()
                .HasOne(c => c.AppUser)
                .WithOne()
                .HasForeignKey<Client>(c => c.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Manager ↔ Venue (N:1)
            modelBuilder.Entity<Manager>()
                .HasOne(m => m.Venue)
                .WithMany(v => v.Managers)
                .HasForeignKey(m => m.VenueId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ticket ↔ Venue (N:1)
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Venue)
                .WithMany(v => v.Tickets)
                .HasForeignKey(t => t.VenueId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ticket ↔ Client (0..1 : N)
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Client)
                .WithMany(c => c.Tickets)
                .HasForeignKey(t => t.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Subscription ↔ Venue (N:1)
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Venue)
                .WithMany(v => v.Subscriptions)
                .HasForeignKey(s => s.VenueId)
                .OnDelete(DeleteBehavior.Cascade);

            // Subscription ↔ Client (N:1)
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Client)
                .WithMany(c => c.Subscriptions)
                .HasForeignKey(s => s.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Sale ↔ Venue (N:1)
            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Venue)
                .WithMany(v => v.Sales)
                .HasForeignKey(s => s.VenueId)
                .OnDelete(DeleteBehavior.Cascade);

            // Sale ↔ Manager (N:1)
            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Manager)
                .WithMany(m => m.Sales)
                .HasForeignKey(s => s.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sale ↔ Ticket (1:1 opcional)
            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Ticket)
                .WithOne(t => t.Sale)
                .HasForeignKey<Sale>(s => s.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sale ↔ Subscription (1:1 opcional)
            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Subscription)
                .WithOne(sub => sub.Sale)
                .HasForeignKey<Sale>(s => s.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
