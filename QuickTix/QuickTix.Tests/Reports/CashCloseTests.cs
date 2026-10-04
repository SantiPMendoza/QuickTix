using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuickTix.Contracts.DTOs.ReportDTOs;
using QuickTix.Contracts.Enums;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;
using QuickTix.DAL.Repositories;

namespace QuickTix.Tests.Reports
{
    /// <summary>
    /// Tests de integración de <see cref="CashCloseRepository"/> (arqueo) contra SQLite in-memory.
    /// Fechas de referencia: julio (CEST, Madrid = UTC+2).
    /// </summary>
    public class CashCloseTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ApplicationDbContext> _options;

        private Venue _pool = null!;
        private Venue _gym = null!;
        private Manager _poolManager = null!;
        private Manager _gymManager = null!;
        private Client _client = null!;

        public CashCloseTests()
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

        private static DateTime Utc(int day, int hour, int minute = 0)
            => new(2026, 7, day, hour, minute, 0, DateTimeKind.Utc);

        /// <summary>Siembra dos recintos con su gestor y un cliente; devuelve un contexto abierto para añadir ventas.</summary>
        private ApplicationDbContext SeedBase()
        {
            var context = new ApplicationDbContext(_options);

            _pool = new Venue { Name = "Piscina", Location = "Nalda", Capacity = 200 };
            _gym = new Venue { Name = "Gimnasio", Location = "Nalda", Capacity = 50 };
            _poolManager = new Manager
            {
                Name = "Gestor Piscina",
                AppUser = new AppUser { UserName = "m1", Name = "Gestor Piscina" },
                Venue = _pool
            };
            _gymManager = new Manager
            {
                Name = "Gestor Gimnasio",
                AppUser = new AppUser { UserName = "m2", Name = "Gestor Gimnasio" },
                Venue = _gym
            };
            _client = new Client { Name = "Cliente Uno", AppUser = new AppUser { UserName = "c1", Name = "Cliente Uno" } };
            context.AddRange(_pool, _gym, _poolManager, _gymManager, _client);
            context.SaveChanges();
            return context;
        }

        private static Sale TicketSale(Venue venue, Manager? manager, DateTime dateUtc, decimal price, int quantity,
            TicketType type = TicketType.AdultoLaboral, TicketContext ticketContext = TicketContext.Normal,
            PaymentMethod method = PaymentMethod.Cash, bool voided = false)
        {
            var sale = new Sale { Venue = venue, Manager = manager, Date = dateUtc, PaymentMethod = method };
            for (var i = 0; i < quantity; i++)
            {
                // Como SellTickets: una línea de cantidad 1 por entrada.
                sale.Items.Add(new SaleItem
                {
                    Ticket = new Ticket
                    {
                        Venue = venue, Price = price, Type = type, Context = ticketContext, PurchaseDate = dateUtc
                    },
                    Quantity = 1,
                    UnitPrice = price
                });
            }
            if (voided)
            {
                sale.VoidedAt = dateUtc.AddMinutes(5);
                sale.VoidReason = "Error de cobro";
            }
            return sale;
        }

        private Sale SubscriptionSale(Venue venue, Manager? manager, DateTime dateUtc, decimal price,
            SubscriptionCategory category = SubscriptionCategory.Adulto,
            SubscriptionDuration duration = SubscriptionDuration.Mensual,
            PaymentMethod method = PaymentMethod.Cash)
            => new()
            {
                Venue = venue,
                Manager = manager,
                Date = dateUtc,
                PaymentMethod = method,
                Items =
                {
                    new SaleItem
                    {
                        Subscription = new Subscription
                        {
                            Venue = venue, Client = _client, Category = category, Duration = duration,
                            Price = price, StartDate = dateUtc, EndDate = dateUtc.AddMonths(1)
                        },
                        Quantity = 1,
                        UnitPrice = price
                    }
                }
            };

        private async Task<CashCloseReportDTO> RunAsync(DateOnly from, DateOnly to)
        {
            using var context = new ApplicationDbContext(_options);
            return await new CashCloseRepository(context).GetReportAsync(from, to);
        }

        [Fact]
        public async Task GroupsByVenueThenSeller_AdminSaleWithoutManagerGoesUnderAdministracion()
        {
            using (var seed = SeedBase())
            {
                seed.AddRange(
                    TicketSale(_pool, _poolManager, Utc(15, 9), 4m, 2),     // 8 € gestor piscina
                    TicketSale(_pool, _poolManager, Utc(15, 10), 4m, 1),    // 4 € gestor piscina (2ª venta)
                    SubscriptionSale(_pool, null, Utc(15, 11), 25m),        // 25 € administración
                    TicketSale(_gym, _gymManager, Utc(15, 12), 6m, 1));     // 6 € gimnasio
                seed.SaveChanges();
            }

            var report = await RunAsync(new DateOnly(2026, 7, 15), new DateOnly(2026, 7, 15));

            Assert.Equal(43m, report.Total);
            Assert.Equal(4, report.SaleCount);
            Assert.Equal(0m, report.VoidedTotal);
            Assert.Equal(0, report.VoidedCount);

            var pool = Assert.Single(report.ByVenue, v => v.VenueName == "Piscina");
            Assert.Equal(37m, pool.Total);
            var manager = Assert.Single(pool.BySeller, s => s.SellerName == "Gestor Piscina");
            Assert.Equal(12m, manager.Total);
            Assert.Equal(2, manager.SaleCount);
            var admin = Assert.Single(pool.BySeller, s => s.SellerName == "Administración");
            Assert.Equal(25m, admin.Total);
            Assert.Equal(1, admin.SaleCount);

            var gym = Assert.Single(report.ByVenue, v => v.VenueName == "Gimnasio");
            Assert.Equal(6m, gym.Total);
            Assert.Equal(report.Total, report.ByVenue.Sum(v => v.Total));
        }

        [Fact]
        public async Task BreaksDownByConceptAndPaymentMethod_WithReadableSpanishConcepts()
        {
            using (var seed = SeedBase())
            {
                seed.AddRange(
                    TicketSale(_pool, _poolManager, Utc(15, 9), 4m, 3, TicketType.NiñoLaboral),
                    TicketSale(_pool, _poolManager, Utc(15, 10), 5m, 1, TicketType.AdultoFestivo, TicketContext.InvitadoAbonado, PaymentMethod.Card),
                    SubscriptionSale(_pool, null, Utc(15, 11), 25m, SubscriptionCategory.FamiliaNumerosa, SubscriptionDuration.Temporada, PaymentMethod.Bizum),
                    SubscriptionSale(_pool, null, Utc(15, 12), 20m, SubscriptionCategory.FamiliaNumerosa, SubscriptionDuration.Temporada));
                seed.SaveChanges();
            }

            var report = await RunAsync(new DateOnly(2026, 7, 15), new DateOnly(2026, 7, 15));

            var kids = Assert.Single(report.ByConcept, c => c.Concept == "Entrada Niño laboral");
            Assert.False(kids.IsSubscription);
            Assert.Equal(3, kids.Quantity);
            Assert.Equal(12m, kids.Total);

            Assert.Single(report.ByConcept, c => c.Concept == "Entrada Adulto festivo (invitado de abonado)" && c.Quantity == 1 && c.Total == 5m);

            var family = Assert.Single(report.ByConcept, c => c.Concept == "Abono Familia numerosa temporada");
            Assert.True(family.IsSubscription);
            Assert.Equal(2, family.Quantity);
            Assert.Equal(45m, family.Total); // mismo concepto con precios distintos: se suma

            Assert.Equal(report.Total, report.ByConcept.Sum(c => c.Total));

            Assert.Equal(12m + 20m, report.ByPaymentMethod.Single(p => p.PaymentMethod == PaymentMethod.Cash).Total);
            Assert.Equal(5m, report.ByPaymentMethod.Single(p => p.PaymentMethod == PaymentMethod.Card).Total);
            Assert.Equal(25m, report.ByPaymentMethod.Single(p => p.PaymentMethod == PaymentMethod.Bizum).Total);
            Assert.Equal(report.Total, report.ByPaymentMethod.Sum(p => p.Total));
        }

        [Fact]
        public async Task SaleAt2330Madrid_BelongsToThatDay_AndSaleAt0030MadridToTheNext()
        {
            using (var seed = SeedBase())
            {
                seed.AddRange(
                    TicketSale(_pool, _poolManager, Utc(15, 21, 30), 7m, 1),   // 23:30 Madrid del 15
                    TicketSale(_pool, _poolManager, Utc(15, 22, 30), 9m, 1));  // 00:30 Madrid del 16
                seed.SaveChanges();
            }

            var day15 = await RunAsync(new DateOnly(2026, 7, 15), new DateOnly(2026, 7, 15));
            var day16 = await RunAsync(new DateOnly(2026, 7, 16), new DateOnly(2026, 7, 16));

            Assert.Equal(7m, day15.Total);
            Assert.Equal(9m, day16.Total);
            var line = Assert.Single(day16.Lines);
            Assert.Equal(new DateTime(2026, 7, 16, 0, 30, 0), line.LocalDateTime);
        }

        [Fact]
        public async Task MultiDayRange_IsInclusiveAtBothEnds_AndLinesAreOrderedByDate()
        {
            using (var seed = SeedBase())
            {
                seed.AddRange(
                    TicketSale(_pool, _poolManager, Utc(14, 21, 59), 1m, 1),   // 23:59 Madrid del 14: fuera
                    TicketSale(_pool, _poolManager, Utc(16, 12), 3m, 1),       // día 16 (se siembra desordenada)
                    TicketSale(_pool, _poolManager, Utc(14, 22, 0), 2m, 1),    // 00:00 Madrid del 15: dentro
                    TicketSale(_pool, _poolManager, Utc(17, 21, 59), 4m, 1),   // 23:59 Madrid del 17: dentro
                    TicketSale(_pool, _poolManager, Utc(17, 22, 0), 8m, 1));   // 00:00 Madrid del 18: fuera
                seed.SaveChanges();
            }

            var report = await RunAsync(new DateOnly(2026, 7, 15), new DateOnly(2026, 7, 17));

            Assert.Equal(9m, report.Total);
            Assert.Equal(3, report.SaleCount);
            Assert.Equal(new DateOnly(2026, 7, 15), report.From);
            Assert.Equal(new DateOnly(2026, 7, 17), report.To);
            Assert.Equal(new[] { 2m, 3m, 4m }, report.Lines.Select(l => l.Subtotal).ToArray());
        }

        [Fact]
        public async Task VoidedSale_IsExcludedFromTotalsAndGroups_ButListedInLinesAndVoidedTotals()
        {
            using (var seed = SeedBase())
            {
                seed.AddRange(
                    TicketSale(_pool, _poolManager, Utc(15, 9), 4m, 2),                                               // 8 € válida
                    TicketSale(_pool, _poolManager, Utc(15, 10), 10m, 1, method: PaymentMethod.Card, voided: true));  // 10 € anulada
                seed.SaveChanges();
            }

            var report = await RunAsync(new DateOnly(2026, 7, 15), new DateOnly(2026, 7, 15));

            Assert.Equal(8m, report.Total);
            Assert.Equal(1, report.SaleCount);
            Assert.Equal(10m, report.VoidedTotal);
            Assert.Equal(1, report.VoidedCount);

            Assert.Equal(8m, report.ByVenue.Sum(v => v.Total));
            Assert.Equal(8m, report.ByVenue.Single().BySeller.Single().Total);
            Assert.Equal(1, report.ByVenue.Single().BySeller.Single().SaleCount);
            Assert.Equal(8m, report.ByConcept.Sum(c => c.Total));
            Assert.DoesNotContain(report.ByPaymentMethod, p => p.PaymentMethod == PaymentMethod.Card);

            var voidedLine = Assert.Single(report.Lines, l => l.IsVoided);
            Assert.Equal(10m, voidedLine.Subtotal);
            Assert.Equal("Error de cobro", voidedLine.VoidReason);
            Assert.Equal(PaymentMethod.Card, voidedLine.PaymentMethod);
            // Líneas no anuladas: 2 entradas del mismo precio de la misma venta = una línea de cantidad 2.
            var validLine = Assert.Single(report.Lines, l => !l.IsVoided);
            Assert.Equal(2, validLine.Quantity);
            Assert.Equal(4m, validLine.UnitPrice);
            Assert.Equal(8m, validLine.Subtotal);
            Assert.Null(validLine.VoidReason);

            // E3: las líneas no anuladas suman el total mostrado.
            Assert.Equal(report.Total, report.Lines.Where(l => !l.IsVoided).Sum(l => l.Subtotal));
        }

        [Fact]
        public async Task EmptyRange_ReturnsZeroedReport()
        {
            SeedBase().Dispose();

            var report = await RunAsync(new DateOnly(2026, 7, 15), new DateOnly(2026, 7, 15));

            Assert.Equal(0m, report.Total);
            Assert.Equal(0, report.SaleCount);
            Assert.Empty(report.ByVenue);
            Assert.Empty(report.Lines);
        }

        [Fact]
        public async Task FromAfterTo_IsRejected()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => RunAsync(new DateOnly(2026, 7, 16), new DateOnly(2026, 7, 15)));
        }

        [Fact]
        public async Task RangeLongerThan366Days_IsRejected_But366IsAccepted()
        {
            var from = new DateOnly(2026, 1, 1);

            await Assert.ThrowsAsync<ArgumentException>(() => RunAsync(from, from.AddDays(366))); // 367 días
            var ok = await RunAsync(from, from.AddDays(365)); // 366 días
            Assert.Equal(0, ok.SaleCount);
        }
    }
}
