using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Contracts.DTOs.SaleDTOs;
using QuickTix.Contracts.Enums;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;
using QuickTix.DAL.Repositories;
using QuickTix.Tests.Time;

namespace QuickTix.Tests.Sales
{
    /// <summary>
    /// Tests de integración de <see cref="SaleRepository.VoidAsync"/> (anulación lógica) y de
    /// su reflejo en el historial y en el Panel, contra SQLite in-memory (ADR-005).
    /// </summary>
    public class VoidSaleTests : IDisposable
    {
        private static readonly DateTime Now = new(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc);

        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public VoidSaleTests()
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

        private static SaleRepository CreateRepository(ApplicationDbContext context, IMemoryCache? cache = null)
        {
            var saleCache = cache ?? new MemoryCache(new MemoryCacheOptions());
            return new SaleRepository(
                context,
                saleCache,
                new PricingRepository(context, new MemoryCache(new MemoryCacheOptions())));
        }

        /// <summary>Siembra una venta de 2 entradas a 4 € (8 €) y otra de abono (25 €, administración).</summary>
        private (int TicketSaleId, int SubscriptionSaleId) SeedSales()
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
                Date = Now,
                Items =
                {
                    new SaleItem
                    {
                        Ticket = new Ticket
                        {
                            Venue = venue, Price = 4m, Type = TicketType.AdultoLaboral,
                            Context = TicketContext.Normal, PurchaseDate = Now
                        },
                        Quantity = 2,
                        UnitPrice = 4m
                    }
                }
            };

            var subscriptionSale = new Sale
            {
                Venue = venue,
                Manager = null,
                Date = Now,
                Items =
                {
                    new SaleItem
                    {
                        Subscription = new Subscription
                        {
                            Venue = venue, Client = client, Category = SubscriptionCategory.Adulto,
                            Duration = SubscriptionDuration.Mensual, Price = 25m,
                            StartDate = Now, EndDate = Now.AddMonths(1)
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

        [Fact]
        public async Task VoidAsync_SetsVoidFieldsAndKeepsPaymentMethodAndItems()
        {
            var (saleId, _) = SeedSales();
            var voidedAt = Now.AddHours(2);

            using (var context = new ApplicationDbContext(_options))
            {
                var result = await CreateRepository(context).VoidAsync(saleId, "admin-user-1", "  Error de cobro  ", voidedAt);
                Assert.Equal(VoidSaleResult.Voided, result);
            }

            using var verify = new ApplicationDbContext(_options);
            var sale = await verify.Sales.Include(s => s.Items).SingleAsync(s => s.Id == saleId);
            Assert.True(sale.IsVoided);
            Assert.Equal(voidedAt, sale.VoidedAt);
            Assert.Equal("admin-user-1", sale.VoidedByUserId);
            Assert.Equal("Error de cobro", sale.VoidReason); // recortado
            Assert.Equal(PaymentMethod.Cash, sale.PaymentMethod);
            Assert.Single(sale.Items); // la venta y sus líneas no se tocan
        }

        [Fact]
        public async Task VoidAsync_SecondVoid_ReturnsAlreadyVoidedAndKeepsOriginalData()
        {
            var (saleId, _) = SeedSales();
            using var context = new ApplicationDbContext(_options);
            var repository = CreateRepository(context);

            await repository.VoidAsync(saleId, "admin-1", "Primer motivo", Now);
            var second = await repository.VoidAsync(saleId, "admin-2", "Segundo motivo", Now.AddHours(1));

            Assert.Equal(VoidSaleResult.AlreadyVoided, second);

            using var verify = new ApplicationDbContext(_options);
            var sale = await verify.Sales.SingleAsync(s => s.Id == saleId);
            Assert.Equal("Primer motivo", sale.VoidReason);
            Assert.Equal("admin-1", sale.VoidedByUserId);
            Assert.Equal(Now, sale.VoidedAt);
        }

        [Fact]
        public async Task VoidAsync_UnknownSale_ReturnsNotFound()
        {
            SeedSales();
            using var context = new ApplicationDbContext(_options);

            var result = await CreateRepository(context).VoidAsync(9999, "admin-1", "Motivo", Now);

            Assert.Equal(VoidSaleResult.NotFound, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task VoidAsync_MissingReason_IsRejectedAndNothingChanges(string? reason)
        {
            var (saleId, _) = SeedSales();
            using var context = new ApplicationDbContext(_options);

            var result = await CreateRepository(context).VoidAsync(saleId, "admin-1", reason!, Now);

            Assert.Equal(VoidSaleResult.InvalidReason, result);
            using var verify = new ApplicationDbContext(_options);
            Assert.Null((await verify.Sales.SingleAsync(s => s.Id == saleId)).VoidedAt);
        }

        [Fact]
        public async Task VoidAsync_ReasonLongerThan200_IsRejected_ButExactly200IsAccepted()
        {
            var (saleId, _) = SeedSales();
            using var context = new ApplicationDbContext(_options);
            var repository = CreateRepository(context);

            var tooLong = await repository.VoidAsync(saleId, "admin-1", new string('x', VoidSaleDTO.ReasonMaxLength + 1), Now);
            Assert.Equal(VoidSaleResult.InvalidReason, tooLong);

            var exact = await repository.VoidAsync(saleId, "admin-1", new string('x', VoidSaleDTO.ReasonMaxLength), Now);
            Assert.Equal(VoidSaleResult.Voided, exact);
        }

        [Fact]
        public async Task VoidAsync_ClearsSaleListCache()
        {
            var (saleId, _) = SeedSales();
            var cache = new MemoryCache(new MemoryCacheOptions());
            using var context = new ApplicationDbContext(_options);
            var repository = CreateRepository(context, cache);

            // Calienta la caché del listado: sin invalidar, GET devolvería la venta sin anular.
            var cached = (await repository.GetAllAsync()).Single(s => s.Id == saleId);
            Assert.False(cached.IsVoided);

            await repository.VoidAsync(saleId, "admin-1", "Motivo", Now);

            var after = (await repository.GetAllAsync()).Single(s => s.Id == saleId);
            Assert.True(after.IsVoided);
        }

        [Fact]
        public async Task TicketHistory_StillListsVoidedSale_WithVoidFields()
        {
            var (ticketSaleId, _) = SeedSales();
            using var context = new ApplicationDbContext(_options);
            var repository = CreateRepository(context);

            await repository.VoidAsync(ticketSaleId, "admin-1", "Cobro duplicado", Now.AddHours(1));
            var history = (await repository.GetTicketHistoryAsync()).ToList();

            var row = Assert.Single(history);
            Assert.Equal(ticketSaleId, row.Id);
            Assert.True(row.IsVoided);
            // El historial expone hora local de Madrid (julio = UTC+2), no la UTC guardada
            Assert.Equal(new DateTime(2026, 7, 15, 13, 0, 0, DateTimeKind.Unspecified), row.VoidedAt);
            Assert.Equal("Cobro duplicado", row.VoidReason);
            Assert.Equal(PaymentMethod.Cash, row.PaymentMethod);
            Assert.Equal(8m, row.TotalAmount);

            var detail = await repository.GetTicketHistoryDetailAsync(ticketSaleId);
            Assert.True(detail.IsVoided);
            Assert.Equal("Cobro duplicado", detail.VoidReason);
        }

        [Fact]
        public async Task SubscriptionHistory_StillListsVoidedSale_WithVoidFields_AndNonVoidedIsClean()
        {
            var (_, subscriptionSaleId) = SeedSales();
            using var context = new ApplicationDbContext(_options);
            var repository = CreateRepository(context);

            var before = (await repository.GetSubscriptionHistoryAsync()).Single();
            Assert.False(before.IsVoided);
            Assert.Null(before.VoidedAt);
            Assert.Null(before.VoidReason);
            Assert.Equal(PaymentMethod.Cash, before.PaymentMethod);

            await repository.VoidAsync(subscriptionSaleId, "admin-1", "Cliente equivocado", Now.AddHours(1));
            var after = (await repository.GetSubscriptionHistoryAsync()).Single();

            Assert.Equal(subscriptionSaleId, after.Id);
            Assert.True(after.IsVoided);
            Assert.Equal("Cliente equivocado", after.VoidReason);
        }

        [Fact]
        public async Task Analytics_ReflectsAVoid_AfterVoidAsyncAndCacheInvalidation()
        {
            var (ticketSaleId, _) = SeedSales();
            var analyticsCache = new MemoryCache(new MemoryCacheOptions());
            using var context = new ApplicationDbContext(_options);
            var analytics = new AnalyticsRepository(context, analyticsCache, new FixedTimeProvider(Now));
            var sales = CreateRepository(context);

            Assert.Equal(33m, (await analytics.GetSummaryAsync()).RevenueToday); // 8 + 25

            await sales.VoidAsync(ticketSaleId, "admin-1", "Motivo", Now);
            // Sin invalidar, el Panel sirve el dato cacheado (TTL 30 s)...
            Assert.Equal(33m, (await analytics.GetSummaryAsync()).RevenueToday);

            // ...tras invalidar (lo que hace el controlador al anular) baja al instante.
            analytics.InvalidateSummaryCache();
            var summary = await analytics.GetSummaryAsync();
            Assert.Equal(25m, summary.RevenueToday);
            Assert.Equal(25m, summary.SeasonRevenue);
            Assert.Equal(0m, summary.TicketRevenueToday);
        }
    }
}
