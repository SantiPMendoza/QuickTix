using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Contracts.Enums;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;
using QuickTix.DAL.Repositories;

namespace QuickTix.Tests.Analytics
{
    /// <summary>
    /// Tests de integración de <see cref="AnalyticsRepository.GetSummaryAsync"/> contra
    /// SQLite in-memory (mismo bootstrap que los tests de ventas: el provider InMemory
    /// de EF queda vetado en este proyecto por no soportar transacciones).
    ///
    /// Cubren los campos nuevos del Panel v2: desglose de ingresos de hoy por tipo,
    /// acumulado de temporada (año en curso, UTC) y abonos que caducan en 7 días.
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

        private static AnalyticsRepository CreateRepository(ApplicationDbContext context)
        {
            // Caché fresca por test: el resumen se cachea 30 s y una caché compartida
            // haría que un test viera los datos sembrados por otro.
            return new AnalyticsRepository(context, new MemoryCache(new MemoryCacheOptions()));
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
        /// Devuelve la fecha de la venta antigua para que el assert de temporada
        /// pueda adaptarse al único día del año en que "ayer" es del año anterior.
        /// </summary>
        private DateTime SeedPanelScenario()
        {
            using var context = new ApplicationDbContext(_options);

            var nowUtc = DateTime.UtcNow;
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

            return olderSaleDateUtc;
        }

        [Fact]
        public async Task GetSummary_SplitsTodayRevenueByLineTypeAndAccumulatesSeason()
        {
            // Arrange
            var olderSaleDateUtc = SeedPanelScenario();
            using var context = new ApplicationDbContext(_options);
            var repository = CreateRepository(context);

            // Act
            var summary = await repository.GetSummaryAsync();

            // Assert — desglose de hoy: 2 x 3,50 € en entradas y 25 € en abonos;
            // la venta antigua no contamina el día.
            Assert.Equal(7.00m, summary.TicketRevenueToday);
            Assert.Equal(25.00m, summary.SubscriptionRevenueToday);
            Assert.Equal(32.00m, summary.RevenueToday);

            // Assert — temporada (año en curso): incluye la venta antigua solo si
            // "ayer" cae en el mismo año (el 1 de enero no lo hace).
            var expectedSeasonRevenue = 32.00m
                + (olderSaleDateUtc.Year == DateTime.UtcNow.Year ? 10.00m : 0m);
            Assert.Equal(expectedSeasonRevenue, summary.SeasonRevenue);
        }

        [Fact]
        public async Task GetSummary_CountsOnlyActiveSubscriptionsExpiringWithin7Days()
        {
            // Arrange
            SeedPanelScenario();
            using var context = new ApplicationDbContext(_options);
            var repository = CreateRepository(context);

            // Act
            var summary = await repository.GetSummaryAsync();

            // Assert — vigentes: el vendido hoy (+60d), el que caduca en 3 días y el
            // que caduca en 30; el caducado no cuenta. De ellos, solo el de 3 días
            // entra en la ventana de 7 días.
            Assert.Equal(3, summary.ActiveSubscriptions);
            Assert.Equal(1, summary.ExpiringSubscriptionsCount);
        }
    }
}
