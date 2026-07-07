using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Contracts.DTOs.SaleDTOs.Subscription;
using QuickTix.Contracts.Enums;
using QuickTix.Core.Models.Entities;
using QuickTix.Core.Models.Entities.Price;
using QuickTix.DAL.Data;
using QuickTix.DAL.Repositories;

namespace QuickTix.Tests.Sales
{
    /// <summary>
    /// Tests de integración de <see cref="SaleRepository.SellSubscriptionAsync"/> para ventas
    /// de administración (ManagerId null) contra SQLite in-memory.
    ///
    /// Usamos SQLite (y no el provider InMemory de EF) porque la venta abre una transacción
    /// real con BeginTransactionAsync: InMemory no soporta transacciones.
    /// </summary>
    public class SellSubscriptionAsAdminTests : IDisposable
    {
        // La BD in-memory de SQLite vive mientras esta conexión siga abierta:
        // todos los DbContext del test la comparten para ver la misma base de datos.
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public SellSubscriptionAsAdminTests()
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

        /// <summary>
        /// Siembra lo mínimo para vender una suscripción SIN manager: un venue, un cliente
        /// (con su AppUser, obligatorio por la relación 1:1) y el precio configurado
        /// (Adulto, Mensual) = 30 €.
        /// </summary>
        private (int VenueId, int ClientId) SeedBaseData()
        {
            using var context = new ApplicationDbContext(_options);

            var venue = new Venue { Name = "Piscina Nalda", Location = "Nalda", Capacity = 200 };
            var user = new AppUser { UserName = "client1", Name = "Cliente Uno" };
            var client = new Client { Name = "Cliente Uno", AppUser = user };

            context.Add(new VenueSubscriptionPrice
            {
                Venue = venue,
                Category = SubscriptionCategory.Adulto,
                Duration = SubscriptionDuration.Mensual,
                Price = 30m
            });
            context.Add(client);
            context.SaveChanges();

            return (venue.Id, client.Id);
        }

        private static SaleRepository CreateRepository(ApplicationDbContext context)
        {
            // Cachés frescas por test: PricingRepository cachea el mapa de precios por venue
            // y una caché compartida entre tests contaminaría los resultados.
            return new SaleRepository(
                context,
                new MemoryCache(new MemoryCacheOptions()),
                new PricingRepository(context, new MemoryCache(new MemoryCacheOptions())));
        }

        private static SellSubscriptionDTO BuildAdminRequest(int venueId, int clientId) => new()
        {
            VenueId = venueId,
            ClientId = clientId,
            ManagerId = null, // venta registrada por administración
            Category = SubscriptionCategory.Adulto,
            Duration = SubscriptionDuration.Mensual,
            StartDate = DateTime.UtcNow.Date
        };

        [Fact]
        public async Task SellSubscription_WithNullManagerId_PersistsSaleWithoutManager()
        {
            // Arrange
            var (venueId, clientId) = SeedBaseData();
            using var context = new ApplicationDbContext(_options);
            var repository = CreateRepository(context);

            // Act
            await repository.SellSubscriptionAsync(BuildAdminRequest(venueId, clientId));

            // Assert — contra un contexto nuevo: comprobamos lo que hay en la BD,
            // no lo que el contexto que vendió cree tener en memoria.
            using var assertContext = new ApplicationDbContext(_options);

            var sale = assertContext.Sales.Include(s => s.Items).Single();
            Assert.Null(sale.ManagerId);
            Assert.Single(sale.Items);
            Assert.Equal(30m, sale.Items.Single().UnitPrice);

            var subscription = assertContext.Subscriptions.Single();
            Assert.Equal(clientId, subscription.ClientId);
            Assert.Equal(venueId, subscription.VenueId);
            Assert.Equal(SubscriptionCategory.Adulto, subscription.Category);
        }

        [Fact]
        public async Task GetSubscriptionHistory_ForAdminSale_ReturnsAdministracionAsManagerName()
        {
            // Arrange
            var (venueId, clientId) = SeedBaseData();
            using var context = new ApplicationDbContext(_options);
            var repository = CreateRepository(context);
            await repository.SellSubscriptionAsync(BuildAdminRequest(venueId, clientId));

            // Act — contra un contexto nuevo para leer lo realmente persistido
            using var queryContext = new ApplicationDbContext(_options);
            var history = await CreateRepository(queryContext).GetSubscriptionHistoryAsync();

            // Assert
            var row = Assert.Single(history);
            Assert.Null(row.ManagerId);
            Assert.Equal("Administración", row.ManagerName);
        }
    }
}
