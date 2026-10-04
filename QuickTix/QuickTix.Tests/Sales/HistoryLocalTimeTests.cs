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
    /// El historial de ventas muestra la hora local de Madrid (igual que el arqueo), no la UTC (E17).
    /// </summary>
    public class HistoryLocalTimeTests : IDisposable
    {
        // 21:30 UTC en verano = 23:30 en Madrid (UTC+2)
        private static readonly DateTime SaleUtc = new(2026, 7, 15, 21, 30, 0, DateTimeKind.Utc);
        private static readonly DateTime VoidedUtc = new(2026, 7, 15, 22, 30, 0, DateTimeKind.Utc);

        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public HistoryLocalTimeTests()
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

        private (int TicketSaleId, int SubscriptionSaleId) Seed()
        {
            using var context = new ApplicationDbContext(_options);

            var venue = new Venue { Name = "Piscina Nalda", Location = "Nalda", Capacity = 200 };
            var managerUser = new AppUser { UserName = "manager1", Name = "Gestor Piscina" };
            var manager = new Manager { Name = "Gestor Piscina", AppUser = managerUser, Venue = venue };
            var clientUser = new AppUser { UserName = "client1", Name = "Cliente Uno" };
            var client = new Client { Name = "Cliente Uno", AppUser = clientUser };

            var ticketSale = new Sale
            {
                Venue = venue,
                Manager = manager,
                Date = SaleUtc,
                VoidedAt = VoidedUtc,
                VoidReason = "Error",
                VoidedByUserId = "admin-1",
                Items =
                {
                    new SaleItem
                    {
                        Ticket = new Ticket
                        {
                            Venue = venue, Price = 4m, Type = TicketType.AdultoLaboral,
                            Context = TicketContext.Normal, PurchaseDate = SaleUtc
                        },
                        Quantity = 1,
                        UnitPrice = 4m
                    }
                }
            };

            var subscriptionSale = new Sale
            {
                Venue = venue,
                Manager = null,
                Date = SaleUtc,
                Items =
                {
                    new SaleItem
                    {
                        Subscription = new Subscription
                        {
                            Venue = venue, Client = client, Category = SubscriptionCategory.Adulto,
                            Duration = SubscriptionDuration.Mensual, Price = 25m,
                            StartDate = SaleUtc, EndDate = SaleUtc.AddMonths(1)
                        },
                        Quantity = 1,
                        UnitPrice = 25m
                    }
                }
            };

            context.AddRange(ticketSale, subscriptionSale);
            context.SaveChanges();
            return (ticketSale.Id, subscriptionSale.Id);
        }

        private SaleRepository CreateRepository(ApplicationDbContext context) =>
            new(context, new MemoryCache(new MemoryCacheOptions()),
                new PricingRepository(context, new MemoryCache(new MemoryCacheOptions())));

        private static readonly DateTime ExpectedLocal = new(2026, 7, 15, 23, 30, 0, DateTimeKind.Unspecified);
        private static readonly DateTime ExpectedVoidedLocal = new(2026, 7, 16, 0, 30, 0, DateTimeKind.Unspecified);

        [Fact]
        public async Task TicketHistory_ShowsMadridLocalTime()
        {
            Seed();
            using var context = new ApplicationDbContext(_options);

            var dto = Assert.Single(await CreateRepository(context).GetTicketHistoryAsync());

            Assert.Equal(ExpectedLocal, dto.Date);
            Assert.Equal(DateTimeKind.Unspecified, dto.Date.Kind);
            Assert.Equal(ExpectedVoidedLocal, dto.VoidedAt);
            // el día de la semana se calcula sobre la fecha local ya convertida
            Assert.Equal(ExpectedLocal.ToString("dddd"), dto.DiaSemanaString);
        }

        [Fact]
        public async Task TicketHistoryDetail_ShowsMadridLocalTime()
        {
            var (saleId, _) = Seed();
            using var context = new ApplicationDbContext(_options);

            var dto = await CreateRepository(context).GetTicketHistoryDetailAsync(saleId);

            Assert.Equal(ExpectedLocal, dto.Date);
            Assert.Equal(ExpectedVoidedLocal, dto.VoidedAt);
        }

        [Fact]
        public async Task SubscriptionHistory_ShowsMadridLocalTime()
        {
            Seed();
            using var context = new ApplicationDbContext(_options);

            var dto = Assert.Single(await CreateRepository(context).GetSubscriptionHistoryAsync());

            Assert.Equal(ExpectedLocal, dto.Date);
            Assert.Equal(DateTimeKind.Unspecified, dto.Date.Kind);
            Assert.Null(dto.VoidedAt);
        }
    }
}
