using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Contracts.DTOs.SaleDTOs.Subscription;
using QuickTix.Contracts.DTOs.SaleDTOs.Ticket;
using QuickTix.Contracts.Enums;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Repositories
{
    /// <summary>
    /// Repositorio de ventas (<see cref="Sale"/>).
    /// 
    /// Además de operaciones CRUD básicas, expone consultas de historial y operaciones de venta
    /// (tickets y suscripciones) que crean entidades relacionadas (<see cref="SaleItem"/>, <see cref="Ticket"/>,
    /// <see cref="Subscription"/>). Estas operaciones se ejecutan dentro de transacciones.
    /// </summary>
    public class SaleRepository : ISaleRepository
    {
        // Contexto EF Core de la aplicación
        private readonly ApplicationDbContext _context;

        // Caché en memoria para acelerar lecturas del listado de ventas
        private readonly IMemoryCache _cache;

        // Repositorio de precios para resolver importes unitarios en ventas
        private readonly IPricingRepository _pricingRepository;

        // Clave de caché para el listado de ventas
        private readonly string _cacheKey = "SaleCacheKey";

        // Tiempo de expiración de la caché (en segundos)
        private readonly int _cacheExpirationTime = 3600;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="SaleRepository"/>.
        /// </summary>
        /// <param name="context">DbContext de la aplicación.</param>
        /// <param name="cache">Caché en memoria.</param>
        /// <param name="pricingRepository">Repositorio de precios para resolver importes unitarios.</param>
        public SaleRepository(ApplicationDbContext context, IMemoryCache cache, IPricingRepository pricingRepository)
        {
            _context = context;
            _cache = cache;
            _pricingRepository = pricingRepository;
        }

        /// <summary>
        /// Persiste los cambios en base de datos.
        /// Si se guarda correctamente, invalida la caché asociada.
        /// </summary>
        /// <returns>True si el guardado se realiza correctamente; en caso contrario, false.</returns>
        public async Task<bool> SaveAsync()
        {
            var result = await _context.SaveChangesAsync() >= 0;
            if (result) ClearCache();
            return result;
        }

        /// <summary>
        /// Invalida la caché de ventas.
        /// </summary>
        public void ClearCache() => _cache.Remove(_cacheKey);

        /// <summary>
        /// Obtiene el listado de ventas con datos básicos (Venue y Manager).
        /// Utiliza caché para mejorar rendimiento en listados frecuentes.
        /// </summary>
        /// <returns>Colección de ventas.</returns>
        public async Task<ICollection<Sale>> GetAllAsync()
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Sale> cachedSales))
                return cachedSales;

            var sales = await _context.Sales
                .AsNoTracking()
                .Include(s => s.Venue)
                .Include(s => s.Manager)
                .OrderByDescending(s => s.Date)
                .ToListAsync();

            _cache.Set(
                _cacheKey,
                sales,
                new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheExpirationTime))
            );

            return sales;
        }

        /// <summary>
        /// Obtiene el detalle completo de una venta.
        /// Incluye Venue, Manager, líneas de venta y sus entidades asociadas (Ticket/Subscription).
        /// Usa split query para evitar explosiones cartesianas en colecciones.
        /// </summary>
        /// <param name="id">Identificador de la venta.</param>
        /// <returns>Venta con detalle si existe; en caso contrario, null.</returns>
        public async Task<Sale?> GetDetailAsync(int id)
        {
            return await _context.Sales
                .AsNoTracking()
                .Include(s => s.Venue)
                .Include(s => s.Manager)
                .Include(s => s.Items).ThenInclude(i => i.Ticket)
                .Include(s => s.Items).ThenInclude(i => i.Subscription)
                .AsSplitQuery()
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        /// <summary>
        /// Obtiene una venta preparada para actualización.
        /// Incluye las líneas (<see cref="SaleItem"/>) para permitir modificaciones sobre el agregado.
        /// </summary>
        /// <param name="id">Identificador de la venta.</param>
        /// <returns>Venta si existe; en caso contrario, null.</returns>
        public async Task<Sale?> GetForUpdateAsync(int id)
        {
            return await _context.Sales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        /// <summary>
        /// Obtiene una venta por id para consulta básica (incluye Venue y Manager).
        /// Si no existe, devuelve null.
        /// </summary>
        /// <param name="id">Identificador de la venta.</param>
        /// <returns>Venta si existe; en caso contrario, null.</returns>
        public async Task<Sale?> GetAsync(int id)
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Sale> cachedSales))
                return cachedSales.FirstOrDefault(s => s.Id == id);

            return await _context.Sales
                .AsNoTracking()
                .Include(s => s.Venue)
                .Include(s => s.Manager)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        /// <summary>
        /// Persiste una actualización de venta.
        /// Convención habitual: la entidad se obtiene previamente con tracking mediante <see cref="GetForUpdateAsync"/>.
        /// </summary>
        /// <param name="sale">Entidad venta.</param>
        /// <returns>True si se guarda correctamente; en caso contrario, false.</returns>
        public async Task<bool> UpdateAsync(Sale sale)
        {
            return await SaveAsync();
        }

        /// <summary>
        /// Indica si existe una venta con el id especificado.
        /// </summary>
        /// <param name="id">Identificador de la venta.</param>
        /// <returns>True si existe; en caso contrario, false.</returns>
        public async Task<bool> ExistsAsync(int id) =>
            await _context.Sales.AnyAsync(s => s.Id == id);

        /// <summary>
        /// Crea una venta y persiste cambios.
        /// </summary>
        /// <param name="sale">Entidad venta.</param>
        /// <returns>True si se guarda correctamente; en caso contrario, false.</returns>
        public async Task<bool> CreateAsync(Sale sale)
        {
            await _context.Sales.AddAsync(sale);
            return await SaveAsync();
        }

        /// <summary>
        /// Elimina una venta por id y persiste cambios.
        /// Incluye las líneas para garantizar la eliminación del agregado completo.
        /// </summary>
        /// <param name="id">Identificador de la venta.</param>
        /// <returns>True si se elimina; false si no existe.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null)
                return false;

            _context.Sales.Remove(sale);
            return await SaveAsync();
        }

        /// <summary>
        /// Obtiene el historial agregado de ventas de tickets.
        /// Devuelve una proyección por venta con cantidades y total monetario.
        /// </summary>
        /// <returns>Listado de ventas de tickets.</returns>
        public async Task<IEnumerable<TicketSaleDTO>> GetTicketHistoryAsync()
        {
            return await _context.Sales
                .AsNoTracking()
                .Where(s => s.Items.Any(i => i.TicketId != null))
                .Select(s => new TicketSaleDTO
                {
                    Id = s.Id,
                    Date = s.Date,

                    VenueId = s.VenueId,
                    VenueName = s.Venue.Name,

                    // Las ventas de tickets siempre llevan manager, pero la proyección
                    // se protege igualmente frente a nulls (FK ahora opcional).
                    ManagerId = s.ManagerId ?? 0,
                    ManagerName = s.Manager != null ? s.Manager.Name : "Administración",

                    Quantity = s.Items
                        .Where(i => i.TicketId != null)
                        .Sum(i => i.Quantity),

                    TotalAmount = s.Items
                        .Where(i => i.TicketId != null)
                        .Sum(i => i.UnitPrice * i.Quantity)
                })
                .OrderByDescending(x => x.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene el detalle de una venta de tickets, agrupando líneas por tipo/contexto/precio unitario.
        /// Incluye información adicional del abonado invitante cuando aplica (InvitadoAbonado).
        /// </summary>
        /// <param name="saleId">Identificador de la venta.</param>
        /// <returns>Detalle de la venta.</returns>
        /// <exception cref="KeyNotFoundException">Si la venta no existe.</exception>
        public async Task<TicketSaleDetailDTO> GetTicketHistoryDetailAsync(int saleId)
        {
            var sale = await _context.Sales
                .AsNoTracking()
                .Include(s => s.Venue)
                .Include(s => s.Manager)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Ticket)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
                throw new KeyNotFoundException("Venta no encontrada.");

            var ticketItems = sale.Items
                .Where(i => i.TicketId != null && i.Ticket != null)
                .ToList();

            var lines = ticketItems
                .GroupBy(i => new { i.Ticket!.Type, i.Ticket!.Context, i.UnitPrice })
                .Select(g => new TicketSaleDetailLineDTO
                {
                    Type = g.Key.Type,
                    Context = g.Key.Context,
                    UnitPrice = g.Key.UnitPrice,
                    Quantity = g.Sum(x => x.Quantity),
                    TotalAmount = g.Sum(x => x.UnitPrice * x.Quantity)
                })
                .OrderBy(x => x.Type)
                .ThenBy(x => x.Context)
                .ToList();

            var invitedClientId = ticketItems
                .Where(i => i.Ticket != null && i.Ticket.Context == TicketContext.InvitadoAbonado)
                .Select(i => i.Ticket!.ClientId)
                .FirstOrDefault(id => id.HasValue);

            string? invitedByName = null;

            if (invitedClientId.HasValue)
            {
                invitedByName = await _context.Clients
                    .AsNoTracking()
                    .Where(c => c.Id == invitedClientId.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();
            }

            var totalQuantity = ticketItems.Sum(i => i.Quantity);
            var totalAmount = ticketItems.Sum(i => i.UnitPrice * i.Quantity);

            return new TicketSaleDetailDTO
            {
                Id = sale.Id,
                Date = sale.Date,

                VenueId = sale.VenueId,
                VenueName = sale.Venue.Name,

                InvitedByClientName = invitedByName,

                // Defensivo: las ventas de tickets siempre llevan manager a día de hoy.
                ManagerId = sale.ManagerId ?? 0,
                ManagerName = sale.Manager != null ? sale.Manager.Name : "Administración",

                Quantity = totalQuantity,
                TotalAmount = totalAmount,

                Lines = lines
            };
        }

        /// <summary>
        /// Obtiene el historial de ventas de suscripciones.
        /// Devuelve una proyección por línea de ítem (suscripción) con información de cliente.
        /// </summary>
        /// <returns>Listado de ventas de suscripciones.</returns>
        public async Task<IEnumerable<SubscriptionSaleDTO>> GetSubscriptionHistoryAsync()
        {
            // La consulta parte de SaleItems (y no de Sales.SelectMany) para que EF la
            // traduzca con JOINs simples: la versión con SelectMany requería APPLY,
            // que SQLite (usado en tests de integración) no soporta.
            var rows = await _context.SaleItems
                .AsNoTracking()
                .Where(i => i.SubscriptionId != null)
                .Select(i => new
                {
                    Id = i.SaleId,
                    i.Sale.Date,

                    i.Sale.VenueId,
                    VenueName = i.Sale.Venue.Name,

                    i.Sale.ManagerId,
                    ManagerName = i.Sale.Manager != null ? i.Sale.Manager.Name : "Administración",

                    SubscriptionCategory = i.Subscription!.Category,
                    Price = i.UnitPrice,

                    ClientName = i.Subscription.Client != null ? i.Subscription.Client.Name : string.Empty
                })
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            return rows.Select(x => new SubscriptionSaleDTO
            {
                Id = x.Id,
                Date = x.Date,

                VenueId = x.VenueId,
                VenueName = x.VenueName,

                ManagerId = x.ManagerId,
                ManagerName = x.ManagerName,

                SubscriptionCategory = x.SubscriptionCategory.ToString(),
                Price = x.Price,

                ClientName = x.ClientName
            });
        }

        /// <summary>
        /// Registra una venta de tickets de una sola línea.
        /// Crea la venta y los tickets asociados, resolviendo el precio unitario desde el mapa de precios.
        /// </summary>
        /// <param name="request">Datos de la venta de tickets.</param>
        /// <returns>Venta creada.</returns>
        public async Task<Sale> SellTicketsAsync(SellTicketDTO request)
        {
            if (request.Context == TicketContext.InvitadoAbonado && !request.ClientId.HasValue)
                throw new ArgumentException("ClientId es obligatorio cuando el contexto es InvitadoAbonado.");

            if (request.Quantity <= 0)
                throw new ArgumentException("Quantity debe ser mayor que cero.");

            var manager = await _context.Managers.AsNoTracking().FirstOrDefaultAsync(m => m.Id == request.ManagerId)
                          ?? throw new ArgumentException("Manager no existe.");

            if (manager.VenueId != request.VenueId)
                throw new ArgumentException("El Manager no pertenece al Venue indicado.");

            var venueExists = await _context.Venues.AsNoTracking().AnyAsync(v => v.Id == request.VenueId);
            if (!venueExists)
                throw new ArgumentException("Venue no existe.");

            var unitPrice = await ResolveTicketUnitPriceAsync(request.VenueId, request.Type, request.Context);

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var sale = new Sale
                {
                    VenueId = request.VenueId,
                    ManagerId = request.ManagerId,
                    Date = DateTime.UtcNow,
                    Items = new List<SaleItem>()
                };

                for (var i = 0; i < request.Quantity; i++)
                {
                    var ticket = new Ticket
                    {
                        VenueId = request.VenueId,
                        ClientId = request.ClientId,
                        Type = request.Type,
                        Context = request.Context,
                        Price = unitPrice,
                        PurchaseDate = DateTime.UtcNow
                    };

                    sale.Items.Add(new SaleItem
                    {
                        Ticket = ticket,
                        Quantity = 1,
                        UnitPrice = unitPrice
                    });
                }

                await _context.Sales.AddAsync(sale);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                ClearCache();
                return sale;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Registra una venta de tickets con múltiples líneas (batch).
        /// Para cada línea, resuelve el precio unitario y genera los tickets correspondientes.
        /// </summary>
        /// <param name="request">Datos de la venta batch.</param>
        /// <returns>Venta creada.</returns>
        public async Task<Sale> SellTicketsBatchAsync(SellTicketsBatchDTO request)
        {
            if (request.Lines == null || request.Lines.Count == 0)
                throw new ArgumentException("Debe incluir al menos una línea de venta.");

            if (request.Lines.Any(l => l.Context == TicketContext.InvitadoAbonado) && !request.ClientId.HasValue)
                throw new ArgumentException("ClientId es obligatorio si hay tickets con Context=InvitadoAbonado.");

            if (request.Lines.Any(l => l.Quantity <= 0))
                throw new ArgumentException("Todas las líneas deben tener Quantity mayor que cero.");

            var manager = await _context.Managers.AsNoTracking().FirstOrDefaultAsync(m => m.Id == request.ManagerId)
                          ?? throw new ArgumentException("Manager no existe.");

            if (manager.VenueId != request.VenueId)
                throw new ArgumentException("El Manager no pertenece al Venue indicado.");

            var venueExists = await _context.Venues.AsNoTracking().AnyAsync(v => v.Id == request.VenueId);
            if (!venueExists)
                throw new ArgumentException("Venue no existe.");

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var sale = new Sale
                {
                    VenueId = request.VenueId,
                    ManagerId = request.ManagerId,
                    Date = DateTime.UtcNow,
                    Items = new List<SaleItem>()
                };

                foreach (var line in request.Lines)
                {
                    var unitPrice = await ResolveTicketUnitPriceAsync(request.VenueId, line.Type, line.Context);

                    var ticketClientId = line.Context == TicketContext.InvitadoAbonado
                        ? request.ClientId
                        : null;

                    for (var i = 0; i < line.Quantity; i++)
                    {
                        var ticket = new Ticket
                        {
                            VenueId = request.VenueId,
                            ClientId = ticketClientId,
                            Type = line.Type,
                            Context = line.Context,
                            Price = unitPrice,
                            PurchaseDate = DateTime.UtcNow
                        };

                        sale.Items.Add(new SaleItem
                        {
                            Ticket = ticket,
                            Quantity = 1,
                            UnitPrice = unitPrice
                        });
                    }
                }

                await _context.Sales.AddAsync(sale);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                ClearCache();
                return sale;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Registra una venta de suscripción.
        /// Crea la suscripción y la venta asociada, resolviendo el precio unitario desde el mapa de precios.
        /// </summary>
        /// <param name="request">Datos de venta de suscripción.</param>
        /// <returns>Venta creada.</returns>
        public async Task<Sale> SellSubscriptionAsync(SellSubscriptionDTO request)
        {
            // ManagerId null = venta registrada por administración: no hay manager
            // que validar y la venta se persiste sin manager asociado.
            if (request.ManagerId.HasValue)
            {
                var manager = await _context.Managers.AsNoTracking().FirstOrDefaultAsync(m => m.Id == request.ManagerId.Value)
                              ?? throw new ArgumentException("Manager no existe.");

                if (manager.VenueId != request.VenueId)
                    throw new ArgumentException("El Manager no pertenece al Venue indicado.");
            }

            var venueExists = await _context.Venues.AsNoTracking().AnyAsync(v => v.Id == request.VenueId);
            if (!venueExists)
                throw new ArgumentException("Venue no existe.");

            var clientExists = await _context.Clients.AsNoTracking().AnyAsync(c => c.Id == request.ClientId);
            if (!clientExists)
                throw new ArgumentException("Client no existe.");

            var unitPrice = await ResolveSubscriptionUnitPriceAsync(request.VenueId, request.Category, request.Duration);
            var endDate = CalculateSubscriptionEndDate(request.StartDate, request.Duration);

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var subscription = new Subscription
                {
                    VenueId = request.VenueId,
                    ClientId = request.ClientId,
                    Category = request.Category,
                    Duration = request.Duration,
                    Price = unitPrice,
                    StartDate = request.StartDate,
                    EndDate = endDate
                };

                var sale = new Sale
                {
                    VenueId = request.VenueId,
                    ManagerId = request.ManagerId,
                    Date = DateTime.UtcNow,
                    Items = new List<SaleItem>
                    {
                        new SaleItem
                        {
                            Subscription = subscription,
                            Quantity = 1,
                            UnitPrice = unitPrice
                        }
                    }
                };

                await _context.Sales.AddAsync(sale);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                ClearCache();
                return sale;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Resuelve el precio unitario de un ticket para un venue, consultando el mapa de precios.
        /// </summary>
        /// <param name="venueId">Identificador del venue.</param>
        /// <param name="type">Tipo de ticket.</param>
        /// <param name="context">Contexto de ticket.</param>
        /// <returns>Precio unitario.</returns>
        private async Task<decimal> ResolveTicketUnitPriceAsync(int venueId, TicketType type, TicketContext context)
        {
            var map = await _pricingRepository.GetVenuePriceMapAsync(venueId);

            var row = map.TicketPrices
                .FirstOrDefault(x => x.VenueId == venueId && x.Type == type && x.Context == context);

            if (row == null || row.Price <= 0m)
                throw new ArgumentException($"No hay precio configurado para Ticket ({type}, {context}) en el VenueId={venueId}.");

            return row.Price;
        }

        /// <summary>
        /// Resuelve el precio unitario de una suscripción para un venue, consultando el mapa de precios.
        /// </summary>
        /// <param name="venueId">Identificador del venue.</param>
        /// <param name="category">Categoría de suscripción.</param>
        /// <param name="duration">Duración de suscripción.</param>
        /// <returns>Precio unitario.</returns>
        private async Task<decimal> ResolveSubscriptionUnitPriceAsync(int venueId, SubscriptionCategory category, SubscriptionDuration duration)
        {
            var map = await _pricingRepository.GetVenuePriceMapAsync(venueId);

            var row = map.SubscriptionPrices
                .FirstOrDefault(x => x.VenueId == venueId && x.Category == category && x.Duration == duration);

            if (row == null || row.Price <= 0m)
                throw new ArgumentException($"No hay precio configurado para Subscription ({category}, {duration}) en el VenueId={venueId}.");

            return row.Price;
        }

        /// <summary>
        /// Calcula la fecha de fin de una suscripción en función de la fecha de inicio y la duración.
        /// </summary>
        /// <param name="startDate">Fecha de inicio.</param>
        /// <param name="duration">Duración.</param>
        /// <returns>Fecha de fin.</returns>
        private static DateTime CalculateSubscriptionEndDate(DateTime startDate, SubscriptionDuration duration)
        {
            return duration switch
            {
                SubscriptionDuration.Quincenal => startDate.AddDays(15),
                SubscriptionDuration.Mensual => startDate.AddMonths(1),
                SubscriptionDuration.Temporada => startDate.AddMonths(3),
                _ => startDate.AddMonths(1)
            };
        }

        /// <summary>
        /// Método obsoleto. Se mantenía para cálculo de precios hardcodeados.
        /// </summary>
        [Obsolete]
        private static decimal CalculateTicketPrice(TicketType type, TicketContext context) => 0m;

        /// <summary>
        /// Método obsoleto. Se mantenía para cálculo de precios hardcodeados.
        /// </summary>
        [Obsolete]
        private static decimal CalculateSubscriptionPrice(SubscriptionCategory category, SubscriptionDuration duration) => 0m;
    }
}
