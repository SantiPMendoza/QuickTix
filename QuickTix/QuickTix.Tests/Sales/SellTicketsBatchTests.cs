using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Contracts.DTOs.SaleDTOs.Ticket;
using QuickTix.Contracts.Enums;
using QuickTix.Core.Models.Entities;
using QuickTix.Core.Models.Entities.Price;
using QuickTix.DAL.Data;
using QuickTix.DAL.Repositories;

namespace QuickTix.Tests.Sales
{
    /// <summary>
    /// Tests de integración de <see cref="SaleRepository.SellTicketsBatchAsync"/> contra SQLite in-memory.
    ///
    /// Usamos SQLite (y no el provider InMemory de EF) porque la venta abre una transacción
    /// real con BeginTransactionAsync: InMemory no soporta transacciones, así que un test
    /// sobre él pasaría en verde aunque la atomicidad estuviera rota.
    /// </summary>
    public class SellTicketsBatchTests : IDisposable
    {
        // La BD in-memory de SQLite vive mientras esta conexión siga abierta:
        // todos los DbContext del test la comparten para ver la misma base de datos.
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public SellTicketsBatchTests()
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
        /// Siembra el mínimo imprescindible para vender: un venue, un manager (con su AppUser,
        /// obligatorio por la relación 1:1) y UN único precio configurado:
        /// (AdultoLaboral, Normal) = 3,50 €. Cualquier otra combinación resuelve a 0 € y
        /// SaleRepository la rechaza — eso es justo lo que explota el test de atomicidad.
        /// </summary>
        private (int VenueId, int ManagerId) SeedBaseData()
        {
            using var context = new ApplicationDbContext(_options);

            var venue = new Venue { Name = "Piscina Nalda", Location = "Nalda", Capacity = 200 };
            var user = new AppUser { UserName = "manager1", Name = "Manager Uno" };
            var manager = new Manager { Name = "Manager Uno", AppUser = user, Venue = venue };

            context.Add(new VenueTicketPrice
            {
                Venue = venue,
                Type = TicketType.AdultoLaboral,
                Context = TicketContext.Normal,
                Price = 3.50m
            });
            context.Add(manager);
            context.SaveChanges();

            return (venue.Id, manager.Id);
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

        [Fact]
        public async Task SellTicketsBatch_WithConfiguredPrice_PersistsSaleWithAllTickets()
        {
            // Arrange
            var (venueId, managerId) = SeedBaseData();
            using var context = new ApplicationDbContext(_options);
            var repository = CreateRepository(context);

            var request = new SellTicketsBatchDTO
            {
                VenueId = venueId,
                ManagerId = managerId,
                Lines = new List<SellTicketLineDTO>
                {
                    new() { Type = TicketType.AdultoLaboral, Context = TicketContext.Normal, Quantity = 3 }
                }
            };

            // Act
            await repository.SellTicketsBatchAsync(request);

            // Assert — contra un contexto nuevo: comprobamos lo que hay en la BD,
            // no lo que el contexto que vendió cree tener en memoria.
            using var assertContext = new ApplicationDbContext(_options);
            var sale = assertContext.Sales.Include(s => s.Items).Single();

            Assert.Equal(3, sale.Items.Count);
            Assert.All(sale.Items, item => Assert.Equal(3.50m, item.UnitPrice));
            Assert.Equal(3, assertContext.Tickets.Count());
        }

        [Fact]
        public async Task SellTicketsBatch_WhenOneLineHasNoPrice_PersistsNothing()
        {
            // Arrange — la línea 1 tiene precio configurado; la línea 2 no.
            var (venueId, managerId) = SeedBaseData();
            using var context = new ApplicationDbContext(_options);
            var repository = CreateRepository(context);

            var request = new SellTicketsBatchDTO
            {
                VenueId = venueId,
                ManagerId = managerId,
                Lines = new List<SellTicketLineDTO>
                {
                    new() { Type = TicketType.AdultoLaboral, Context = TicketContext.Normal, Quantity = 2 },
                    new() { Type = TicketType.NiñoLaboral, Context = TicketContext.Normal, Quantity = 1 }
                }
            };

            // Act
            await Assert.ThrowsAsync<ArgumentException>(() => repository.SellTicketsBatchAsync(request));

            // Assert — el invariante de negocio: una venta que falla a mitad no deja NADA
            // en la base de datos. Ni la venta, ni las líneas, ni los tickets de la línea buena.
            using var assertContext = new ApplicationDbContext(_options);
            Assert.Equal(0, assertContext.Sales.Count());
            Assert.Equal(0, assertContext.SaleItems.Count());
            Assert.Equal(0, assertContext.Tickets.Count());
        }
    }
}
