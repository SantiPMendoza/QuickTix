using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuickTix.Core.Models.Entities;
using QuickTix.Core.Models.Entities.Price;

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
        public DbSet<SaleItem> SaleItems { get; set; }

        public DbSet<VenueTicketPrice> VenueTicketPrices => Set<VenueTicketPrice>();
        public DbSet<VenueSubscriptionPrice> VenueSubscriptionPrices => Set<VenueSubscriptionPrice>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unicidad global en Identity (AspNetUsers) para NIF y PhoneNumber
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.HasIndex(u => u.Nif)
                      .IsUnique()
                      .HasFilter("[Nif] IS NOT NULL AND [Nif] <> ''");

                entity.HasIndex(u => u.PhoneNumber)
                      .IsUnique()
                      .HasFilter("[PhoneNumber] IS NOT NULL AND [PhoneNumber] <> ''");
            });


            // Relaciones 1:1 de usuarios
            modelBuilder.Entity<Admin>()
                .HasOne(a => a.AppUser)
                .WithOne()
                .HasForeignKey<Admin>(a => a.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Manager>()
                .HasOne(m => m.AppUser)
                .WithOne()
                .HasForeignKey<Manager>(m => m.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Client>()
                .HasOne(c => c.AppUser)
                .WithOne()
                .HasForeignKey<Client>(c => c.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Manager ↔ Venue
            modelBuilder.Entity<Manager>()
                .HasOne(m => m.Venue)
                .WithMany(v => v.Managers)
                .HasForeignKey(m => m.VenueId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ticket ↔ Venue
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Venue)
                .WithMany(v => v.Tickets)
                .HasForeignKey(t => t.VenueId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ticket ↔ Client
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Client)
                .WithMany(c => c.Tickets)
                .HasForeignKey(t => t.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Subscription ↔ Venue
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Venue)
                .WithMany(v => v.Subscriptions)
                .HasForeignKey(s => s.VenueId)
                .OnDelete(DeleteBehavior.Cascade);

            // Subscription ↔ Client
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Client)
                .WithMany(c => c.Subscriptions)
                .HasForeignKey(s => s.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Sale ↔ Venue
            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Venue)
                .WithMany(v => v.Sales)
                .HasForeignKey(s => s.VenueId)
                .OnDelete(DeleteBehavior.Cascade);

            // Sale ↔ Manager
            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Manager)
                .WithMany(m => m.Sales)
                .HasForeignKey(s => s.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sale ↔ SaleItem
            modelBuilder.Entity<Sale>()
                .HasMany(s => s.Items)
                .WithOne(i => i.Sale)
                .HasForeignKey(i => i.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            // SaleItem ↔ Ticket
            modelBuilder.Entity<SaleItem>()
                .HasOne(i => i.Ticket)
                .WithMany()
                .HasForeignKey(i => i.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            // SaleItem ↔ Subscription
            modelBuilder.Entity<SaleItem>()
                .HasOne(i => i.Subscription)
                .WithMany()
                .HasForeignKey(i => i.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);


            // Prices
            modelBuilder.Entity<VenueTicketPrice>()
                .HasIndex(x => new { x.VenueId, x.Type, x.Context })
                .IsUnique();

            modelBuilder.Entity<VenueSubscriptionPrice>()
                .HasIndex(x => new { x.VenueId, x.Category, x.Duration })
                .IsUnique();


            // Decimales: evitar truncados y warnings
            modelBuilder.Entity<VenueTicketPrice>()
                .Property(x => x.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<VenueSubscriptionPrice>()
                .Property(x => x.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Ticket>()
                .Property(x => x.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Subscription>()
                .Property(x => x.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleItem>()
                .Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

        }
    }
}
