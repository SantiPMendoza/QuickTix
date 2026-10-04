using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Contracts.Enums;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;
using QuickTix.DAL.Repositories;

namespace QuickTix.Tests.Sales
{
    /// <summary>
    /// Un Ticket, un Subscription o un Venue con ventas asociadas no se puede borrar:
    /// borrarlos arrastraría SaleItems/Sales y falsearía el arqueo sin rastro (E14, E16).
    /// </summary>
    public class DeleteGuardTests : IDisposable
    {
        private static readonly DateTime Now = new(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc);

        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public DeleteGuardTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = new ApplicationDbContext(_options);
            context.Database.EnsureCreated();
        }

        public void Dispose() => _connection.Dispose();

        private static IMemoryCache NewCache() => new MemoryCache(new MemoryCacheOptions());

        /// <summary>Siembra una venta con un ticket, una con un abono y un ticket/abono/recinto sin ventas.</summary>
        private (int SoldTicketId, int SoldSubscriptionId, int SoldVenueId, int FreeTicketId, int FreeSubscriptionId, int FreeVenueId) Seed()
        {
            using var context = new ApplicationDbContext(_options);

            var venue = new Venue { Name = "Piscina Nalda", Location = "Nalda", Capacity = 200 };
            var freeVenue = new Venue { Name = "Pabellon Vacio", Location = "Nalda", Capacity = 50 };
            var clientUser = new AppUser { UserName = "client1", Name = "Cliente Uno" };
            var client = new Client { Name = "Cliente Uno", AppUser = clientUser };

            var soldTicket = new Ticket
            {
                Venue = venue, Price = 4m, Type = TicketType.AdultoLaboral,
                Context = TicketContext.Normal, PurchaseDate = Now
            };
            var soldSubscription = new Subscription
            {
                Venue = venue, Client = client, Category = SubscriptionCategory.Adulto,
                Duration = SubscriptionDuration.Mensual, Price = 25m,
                StartDate = Now, EndDate = Now.AddMonths(1)
            };
            var freeTicket = new Ticket
            {
                Venue = freeVenue, Price = 4m, Type = TicketType.AdultoLaboral,
                Context = TicketContext.Normal, PurchaseDate = Now
            };
            var freeSubscription = new Subscription
            {
                Venue = freeVenue, Client = client, Category = SubscriptionCategory.Adulto,
                Duration = SubscriptionDuration.Mensual, Price = 25m,
                StartDate = Now, EndDate = Now.AddMonths(1)
            };

            var sale = new Sale
            {
                Venue = venue,
                Manager = null,
                Date = Now,
                Items =
                {
                    new SaleItem { Ticket = soldTicket, Quantity = 1, UnitPrice = 4m },
                    new SaleItem { Subscription = soldSubscription, Quantity = 1, UnitPrice = 25m }
                }
            };

            // freeVenue no tiene ventas pero sí ticket/abono propios (se borran en cascada con el recinto)
            context.AddRange(sale, freeTicket, freeSubscription);
            context.SaveChanges();

            return (soldTicket.Id, soldSubscription.Id, venue.Id, freeTicket.Id, freeSubscription.Id, freeVenue.Id);
        }

        [Fact]
        public async Task TicketDelete_WithSaleItems_IsRefusedAndNothingIsRemoved()
        {
            var ids = Seed();

            using (var context = new ApplicationDbContext(_options))
            {
                var repository = new TicketRepository(context, NewCache());
                await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeleteAsync(ids.SoldTicketId));
            }

            using var verify = new ApplicationDbContext(_options);
            Assert.True(await verify.Tickets.AnyAsync(t => t.Id == ids.SoldTicketId));
            Assert.Equal(2, await verify.SaleItems.CountAsync());
        }

        [Fact]
        public async Task TicketDelete_WithoutSaleItems_IsDeleted()
        {
            var ids = Seed();

            using (var context = new ApplicationDbContext(_options))
            {
                var repository = new TicketRepository(context, NewCache());
                Assert.True(await repository.DeleteAsync(ids.FreeTicketId));
            }

            using var verify = new ApplicationDbContext(_options);
            Assert.False(await verify.Tickets.AnyAsync(t => t.Id == ids.FreeTicketId));
        }

        [Fact]
        public async Task SubscriptionDelete_WithSaleItems_IsRefusedAndNothingIsRemoved()
        {
            var ids = Seed();

            using (var context = new ApplicationDbContext(_options))
            {
                var repository = new SubscriptionRepository(context, NewCache());
                await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeleteAsync(ids.SoldSubscriptionId));
            }

            using var verify = new ApplicationDbContext(_options);
            Assert.True(await verify.Subscriptions.AnyAsync(s => s.Id == ids.SoldSubscriptionId));
            Assert.Equal(2, await verify.SaleItems.CountAsync());
        }

        [Fact]
        public async Task SubscriptionDelete_WithoutSaleItems_IsDeleted()
        {
            var ids = Seed();

            using (var context = new ApplicationDbContext(_options))
            {
                var repository = new SubscriptionRepository(context, NewCache());
                Assert.True(await repository.DeleteAsync(ids.FreeSubscriptionId));
            }

            using var verify = new ApplicationDbContext(_options);
            Assert.False(await verify.Subscriptions.AnyAsync(s => s.Id == ids.FreeSubscriptionId));
        }

        [Fact]
        public async Task VenueDelete_WithSales_IsRefusedAndNothingIsRemoved()
        {
            var ids = Seed();

            using (var context = new ApplicationDbContext(_options))
            {
                var repository = new VenueRepository(context, NewCache());
                await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeleteAsync(ids.SoldVenueId));
            }

            using var verify = new ApplicationDbContext(_options);
            Assert.True(await verify.Venues.AnyAsync(v => v.Id == ids.SoldVenueId));
            Assert.Equal(1, await verify.Sales.CountAsync());
            Assert.Equal(2, await verify.SaleItems.CountAsync());
        }

        [Fact]
        public async Task VenueDelete_WithoutSales_IsDeleted()
        {
            var ids = Seed();

            using (var context = new ApplicationDbContext(_options))
            {
                var repository = new VenueRepository(context, NewCache());
                Assert.True(await repository.DeleteAsync(ids.FreeVenueId));
            }

            using var verify = new ApplicationDbContext(_options);
            Assert.False(await verify.Venues.AnyAsync(v => v.Id == ids.FreeVenueId));
        }

        /// <summary>
        /// Venta en el recinto B que referencia un ticket y un abono del recinto A (los precios/ítems no están
        /// atados al recinto de la venta): borrar A no debe llevarse esos SaleItems por la cascada de Venue→Ticket.
        /// </summary>
        private (int VenueAId, int ClientId) SeedCrossVenueSale(bool viaSubscription)
        {
            using var context = new ApplicationDbContext(_options);

            var venueA = new Venue { Name = "Piscina A", Location = "Nalda", Capacity = 100 };
            var venueB = new Venue { Name = "Pabellon B", Location = "Nalda", Capacity = 100 };
            var client = new Client { Name = "Cliente Dos", AppUser = new AppUser { UserName = "client2", Name = "Cliente Dos" } };

            var item = viaSubscription
                ? new SaleItem
                {
                    Subscription = new Subscription
                    {
                        Venue = venueA, Client = client, Category = SubscriptionCategory.Adulto,
                        Duration = SubscriptionDuration.Mensual, Price = 25m,
                        StartDate = Now, EndDate = Now.AddMonths(1)
                    },
                    Quantity = 1, UnitPrice = 25m
                }
                : new SaleItem
                {
                    Ticket = new Ticket
                    {
                        Venue = venueA, Price = 4m, Type = TicketType.AdultoLaboral,
                        Context = TicketContext.Normal, PurchaseDate = Now
                    },
                    Quantity = 1, UnitPrice = 4m
                };

            context.Add(new Sale { Venue = venueB, Manager = null, Date = Now, Items = { item } });
            context.SaveChanges();
            return (venueA.Id, client.Id);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task VenueDelete_WhoseTicketOrSubscriptionIsSoldInAnotherVenue_FailsAndKeepsSaleItems(bool viaSubscription)
        {
            var seed = SeedCrossVenueSale(viaSubscription);

            using (var context = new ApplicationDbContext(_options))
            {
                var repository = new VenueRepository(context, NewCache());
                await Assert.ThrowsAsync<DbUpdateException>(() => repository.DeleteAsync(seed.VenueAId));
            }

            using var verify = new ApplicationDbContext(_options);
            Assert.True(await verify.Venues.AnyAsync(v => v.Id == seed.VenueAId));
            Assert.Equal(1, await verify.Sales.CountAsync());
            Assert.Equal(1, await verify.SaleItems.CountAsync());
        }

        [Fact]
        public async Task ClientDelete_WithSoldSubscription_FailsAndRemovesNothing()
        {
            var seed = SeedCrossVenueSale(viaSubscription: true);

            using (var context = new ApplicationDbContext(_options))
            {
                var repository = new ClientRepository(context, NewCache());
                await Assert.ThrowsAsync<DbUpdateException>(() => repository.DeleteAsync(seed.ClientId));
            }

            using var verify = new ApplicationDbContext(_options);
            Assert.True(await verify.Clients.AnyAsync(c => c.Id == seed.ClientId));
            Assert.Equal(1, await verify.Subscriptions.CountAsync());
            Assert.Equal(1, await verify.Sales.CountAsync());
            Assert.Equal(1, await verify.SaleItems.CountAsync());
        }
    }
}
