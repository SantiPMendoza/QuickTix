using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Contracts.Enums;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;
using QuickTix.DAL.Repositories;
using QuickTix.Tests.Time;

namespace QuickTix.Tests.Analytics
{
    /// <summary>
    /// Tests de integración de <see cref="AnalyticsRepository.GetSummaryAsync"/> contra
    /// SQLite in-memory (mismo bootstrap que los tests de ventas: el provider InMemory
    /// de EF queda vetado en este proyecto por no soportar transacciones).
    ///
    /// Cubren los campos nuevos del Panel v2: desglose de ingresos de hoy por tipo,
    /// acumulado de temporada (año natural LOCAL de Madrid en curso) y abonos que caducan en 7 días.
    /// </summary>
    public class AnalyticsSummaryTests : IDisposable
    {
        // La BD in-memory de SQLite vive mientras esta conexión siga abierta:
        // todos los DbContext del test la comparten para ver la misma base de datos.
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public AnalyticsSummaryTests()
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

        // El reloj es obligatorio: un TimeProvider.System por defecto haría los tests dependientes del día en que se ejecutan.
        private static AnalyticsRepository CreateRepository(
            ApplicationDbContext context, TimeProvider clock, IMemoryCache? cache = null)
        {
            // Caché fresca por test: el resumen se cachea 30 s y una caché compartida
            // haría que un test viera los datos sembrados por otro.
            return new AnalyticsRepository(
                context,
                cache ?? new MemoryCache(new MemoryCacheOptions()),
                clock);
        }

        private static Sale BuildTicketSale(Venue venue, DateTime dateUtc, decimal price, int quantity, bool voided = false)
        {
            var sale = new Sale
            {
                Venue = venue,
                Date = dateUtc,
                Items =
                {
                    new SaleItem
                    {
                        Ticket = new Ticket
                        {
                            Venue = venue,
                            Price = price,
                            Type = TicketType.AdultoLaboral,
                            Context = TicketContext.Normal,
                            PurchaseDate = dateUtc
                        },
                        Quantity = quantity,
                        UnitPrice = price
                    }
                }
            };
            if (voided)
            {
                sale.VoidedAt = dateUtc;
                sale.VoidReason = "Error de cobro";
            }
            return sale;
        }

        /// <summary>
        /// Siembra el escenario completo del Panel v2:
        /// - Venta de HOY con entradas: 2 x 3,50 € = 7,00 €.
        /// - Venta de HOY (administración, sin manager) con un abono: 25,00 €.
        /// - Venta ANTIGUA (día anterior) con una entrada de 10,00 €: fuera de "hoy",
        ///   dentro de la temporada si el día anterior cae en el mismo año.
        /// - Abonos sueltos: uno vigente que caduca en 3 días (cuenta como "caduca pronto"),
        ///   uno vigente que caduca en 30 días (activo pero fuera de la ventana) y
        ///   uno ya caducado (no cuenta para nada).
        /// La fecha de la venta antigua ("ayer" en UTC) cae en el año local anterior cuando "ahora" es
        /// Nochevieja a las 23:30 UTC (ya 1 de enero en Madrid); los tests fijan el total de temporada esperado.
        /// </summary>
        private void SeedPanelScenario(DateTime nowUtc)
        {
            using var context = new ApplicationDbContext(_options);

            // "Ahora" lo fija el test: es el mismo instante que ve el reloj del repositorio.
            var todayUtc = nowUtc.Date;
            var olderSaleDateUtc = todayUtc.AddDays(-1).AddHours(12);

            var venue = new Venue { Name = "Piscina Nalda", Location = "Nalda", Capacity = 200 };
            var managerUser = new AppUser { UserName = "manager1", Name = "Manager Uno" };
            var manager = new Manager { Name = "Manager Uno", AppUser = managerUser, Venue = venue };
            var clientUser = new AppUser { UserName = "client1", Name = "Cliente Uno" };
            var client = new Client { Name = "Cliente Uno", AppUser = clientUser };

            // --- Venta de hoy: 2 entradas a 3,50 € ---
            var ticketToday = new Ticket
            {
                Venue = venue,
                Price = 3.50m,
                Type = TicketType.AdultoLaboral,
                Context = TicketContext.Normal,
                PurchaseDate = nowUtc
            };
            var ticketSaleToday = new Sale
            {
                Venue = venue,
                Manager = manager,
                Date = nowUtc,
                Items = { new SaleItem { Ticket = ticketToday, Quantity = 2, UnitPrice = 3.50m } }
            };

            // --- Venta de hoy (administración, ManagerId null): 1 abono de 25 € ---
            var subscriptionSoldToday = new Subscription
            {
                Venue = venue,
                Client = client,
                Category = SubscriptionCategory.Adulto,
                Duration = SubscriptionDuration.Mensual,
                Price = 25m,
                StartDate = todayUtc,
                EndDate = nowUtc.AddDays(60)
            };
            var subscriptionSaleToday = new Sale
            {
                Venue = venue,
                Manager = null,
                Date = nowUtc,
                Items = { new SaleItem { Subscription = subscriptionSoldToday, Quantity = 1, UnitPrice = 25m } }
            };

            // --- Venta antigua (día anterior): 1 entrada de 10 € ---
            var olderTicket = new Ticket
            {
                Venue = venue,
                Price = 10m,
                Type = TicketType.AdultoLaboral,
                Context = TicketContext.Normal,
                PurchaseDate = olderSaleDateUtc
            };
            var olderSale = new Sale
            {
                Venue = venue,
                Manager = manager,
                Date = olderSaleDateUtc,
                Items = { new SaleItem { Ticket = olderTicket, Quantity = 1, UnitPrice = 10m } }
            };

            // --- Abonos sueltos para el KPI de caducidad ---
            var expiringSoon = new Subscription
            {
                Venue = venue,
                Client = client,
                Category = SubscriptionCategory.Adulto,
                Duration = SubscriptionDuration.Quincenal,
                Price = 15m,
                StartDate = nowUtc.AddDays(-30),
                EndDate = nowUtc.AddDays(3)
            };
            var activeFarFromExpiry = new Subscription
            {
                Venue = venue,
                Client = client,
                Category = SubscriptionCategory.Adulto,
                Duration = SubscriptionDuration.Mensual,
                Price = 30m,
                StartDate = nowUtc.AddDays(-30),
                EndDate = nowUtc.AddDays(30)
            };
            var alreadyExpired = new Subscription
            {
                Venue = venue,
                Client = client,
                Category = SubscriptionCategory.Adulto,
                Duration = SubscriptionDuration.Mensual,
                Price = 30m,
                StartDate = nowUtc.AddDays(-60),
                EndDate = nowUtc.AddDays(-1)
            };

            context.AddRange(
                ticketSaleToday, subscriptionSaleToday, olderSale,
                expiringSoon, activeFarFromExpiry, alreadyExpired);
            context.SaveChanges();
        }

        // Instantes fijos: un día normal y Nochevieja a las 23:30 UTC, que en Madrid (UTC+1) ya es
        // 1 de enero (año nuevo local mientras el año UTC sigue siendo el anterior).
        [Theory]
        [InlineData("2026-07-15T10:00:00Z", 42)]
        [InlineData("2026-12-31T23:30:00Z", 32)]
        public async Task GetSummary_SplitsTodayRevenueByLineTypeAndAccumulatesSeason(string nowIso, int expectedSeasonRevenueEuros)
        {
            // Arrange
            var nowUtc = DateTime.Parse(nowIso, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
            SeedPanelScenario(nowUtc);
            using var context = new ApplicationDbContext(_options);
            var repository = CreateRepository(context, new FixedTimeProvider(nowUtc));

            // Act
            var summary = await repository.GetSummaryAsync();

            // Assert — desglose de hoy: 2 x 3,50 € en entradas y 25 € en abonos;
            // la venta antigua no contamina el día.
            Assert.Equal(7.00m, summary.TicketRevenueToday);
            Assert.Equal(25.00m, summary.SubscriptionRevenueToday);
            Assert.Equal(32.00m, summary.RevenueToday);

            // Assert — temporada (año LOCAL de Madrid en curso), con el total esperado fijado a mano:
            // en julio entra la venta antigua (32 + 10); en Nochevieja a las 23:30 UTC ya es 1 de enero
            // en Madrid, así que la venta del día anterior cae en el año local anterior (solo 32).
            Assert.Equal((decimal)expectedSeasonRevenueEuros, summary.SeasonRevenue);
        }

        [Fact]
        public async Task GetSummary_CountsOnlyActiveSubscriptionsExpiringWithin7Days()
        {
            // Arrange
            var nowUtc = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc);
            SeedPanelScenario(nowUtc);
            using var context = new ApplicationDbContext(_options);
            var repository = CreateRepository(context, new FixedTimeProvider(nowUtc));

            // Act
            var summary = await repository.GetSummaryAsync();

            // Assert — vigentes: el vendido hoy (+60d), el que caduca en 3 días y el
            // que caduca en 30; el caducado no cuenta. De ellos, solo el de 3 días
            // entra en la ventana de 7 días.
            Assert.Equal(3, summary.ActiveSubscriptions);
            Assert.Equal(1, summary.ExpiringSubscriptionsCount);
        }

        [Fact]
        public async Task GetSummary_ExcludesVoidedSalesFromRevenueSeasonAndSalesByType()
        {
            var now = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc);
            using (var seed = new ApplicationDbContext(_options))
            {
                var venue = new Venue { Name = "Piscina Nalda", Location = "Nalda", Capacity = 200 };
                seed.AddRange(
                    BuildTicketSale(venue, now, 3.50m, 2),
                    BuildTicketSale(venue, now, 10m, 1, voided: true));
                seed.SaveChanges();
            }

            using var context = new ApplicationDbContext(_options);
            var summary = await CreateRepository(context, new FixedTimeProvider(now)).GetSummaryAsync();

            Assert.Equal(7.00m, summary.RevenueToday);
            Assert.Equal(7.00m, summary.TicketRevenueToday);
            Assert.Equal(7.00m, summary.SeasonRevenue);
            Assert.Equal(2, summary.TicketsSoldToday);
            Assert.Equal(2, summary.EstimatedAttendanceToday);
            Assert.Equal(2, summary.SalesByType.TicketUnits);
            Assert.DoesNotContain(summary.RecentSales, s => s.TotalAmount == 10m);
            Assert.Equal(7.00m, summary.RevenueLast7Days.Sum(d => d.Amount));
        }

        [Fact]
        public async Task GetSummary_TodayIsTheMadridDay_NotTheUtcDay()
        {
            // Ahora = 00:30 en Madrid del 16 (22:30 UTC del 15, verano).
            var now = new DateTime(2026, 7, 15, 22, 30, 0, DateTimeKind.Utc);
            using (var seed = new ApplicationDbContext(_options))
            {
                var venue = new Venue { Name = "Piscina Nalda", Location = "Nalda", Capacity = 200 };
                seed.AddRange(
                    BuildTicketSale(venue, new DateTime(2026, 7, 15, 22, 15, 0, DateTimeKind.Utc), 5m, 1), // 00:15 Madrid del 16: hoy
                    BuildTicketSale(venue, new DateTime(2026, 7, 15, 21, 30, 0, DateTimeKind.Utc), 8m, 1)); // 23:30 Madrid del 15: ayer
                seed.SaveChanges();
            }

            using var context = new ApplicationDbContext(_options);
            var summary = await CreateRepository(context, new FixedTimeProvider(now)).GetSummaryAsync();

            Assert.Equal(5m, summary.RevenueToday);
            Assert.Equal(1, summary.TicketsSoldToday);
            Assert.Equal(13m, summary.SeasonRevenue);

            // Serie de 7 días: termina en el día local de hoy (16) y la venta de 23:30 cae en el 15.
            Assert.Equal(7, summary.RevenueLast7Days.Count);
            Assert.Equal(new DateTime(2026, 7, 16), summary.RevenueLast7Days[6].Date);
            Assert.Equal(5m, summary.RevenueLast7Days[6].Amount);
            Assert.Equal(8m, summary.RevenueLast7Days[5].Amount);
        }

        [Fact]
        public async Task InvalidateSummaryCache_MakesNextSummaryReflectAVoid()
        {
            var now = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc);
            int saleId;
            using (var seed = new ApplicationDbContext(_options))
            {
                var venue = new Venue { Name = "Piscina Nalda", Location = "Nalda", Capacity = 200 };
                var sale = BuildTicketSale(venue, now, 4m, 2);
                seed.Add(sale);
                seed.SaveChanges();
                saleId = sale.Id;
            }

            var cache = new MemoryCache(new MemoryCacheOptions());
            using var context = new ApplicationDbContext(_options);
            var repository = CreateRepository(context, new FixedTimeProvider(now), cache);

            var before = await repository.GetSummaryAsync();
            Assert.Equal(8m, before.RevenueToday);

            using (var voidContext = new ApplicationDbContext(_options))
            {
                var sale = voidContext.Sales.Single(s => s.Id == saleId);
                sale.VoidedAt = now;
                sale.VoidReason = "Error de cobro";
                voidContext.SaveChanges();
            }

            // Sin invalidar, la caché sigue sirviendo el dato anterior (TTL de 30 s).
            Assert.Equal(8m, (await repository.GetSummaryAsync()).RevenueToday);

            repository.InvalidateSummaryCache();

            Assert.Equal(0m, (await repository.GetSummaryAsync()).RevenueToday);
        }
    }
}
