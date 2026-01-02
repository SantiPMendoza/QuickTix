using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Contracts.Enums;
using QuickTix.Contracts.Models.DTOs.SaleDTO;
using QuickTix.Contracts.Models.DTOs.SalesHistory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickTix.DAL.Repositories
{
    public class SaleRepository : ISaleRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly string _cacheKey = "SaleCacheKey";
        private readonly int _cacheExpirationTime = 3600;

        public SaleRepository(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<bool> SaveAsync()
        {
            var result = await _context.SaveChangesAsync() >= 0;
            if (result) ClearCache();
            return result;
        }

        public void ClearCache() => _cache.Remove(_cacheKey);

        public async Task<ICollection<Sale>> GetAllAsync()
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Sale> cachedSales))
                return cachedSales;

            // Listado ligero (sin Items)
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

        // Detalle completo (no cache)
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

        // Para edición (tracking)
        public async Task<Sale?> GetForUpdateAsync(int id)
        {
            return await _context.Sales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

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

        public async Task<bool> UpdateAsync(Sale sale)
        {
            return await SaveAsync();
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Sales.AnyAsync(s => s.Id == id);

        public async Task<bool> CreateAsync(Sale sale)
        {
            await _context.Sales.AddAsync(sale);
            return await SaveAsync();
        }

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

        public async Task<IEnumerable<TicketSaleDTO>> GetTicketHistoryAsync()
        {
            return await _context.Sales
                .AsNoTracking()
                .SelectMany(s => s.Items
                    .Where(i => i.TicketId != null)
                    .Select(i => new TicketSaleDTO
                    {
                        Id = s.Id,
                        Date = s.Date,

                        VenueId = s.VenueId,
                        VenueName = s.Venue.Name,

                        ManagerId = s.ManagerId,
                        ManagerName = s.Manager.Name,

                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }))
                .OrderByDescending(x => x.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<SubscriptionSaleDTO>> GetSubscriptionHistoryAsync()
        {
            var rows = await _context.Sales
                .AsNoTracking()
                .SelectMany(s => s.Items
                    .Where(i => i.SubscriptionId != null)
                    .Select(i => new
                    {
                        Id = s.Id,
                        s.Date,

                        s.VenueId,
                        VenueName = s.Venue.Name,

                        s.ManagerId,
                        ManagerName = s.Manager.Name,

                        SubscriptionCategory = i.Subscription.Category,
                        Price = i.UnitPrice,

                        ClientName = i.Subscription.Client != null ? i.Subscription.Client.Name : string.Empty
                    }))
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


        public async Task<Sale> SellTicketsAsync(SellTicketDTO request)
        {
            if (request.Context == TicketContext.InvitadoAbonado && !request.ClientId.HasValue)
                throw new ArgumentException("ClientId es obligatorio cuando el contexto es InvitadoAbonado.");

            if (request.Quantity <= 0)
                throw new ArgumentException("Quantity debe ser mayor que cero.");

            // Validaciones mínimas de integridad
            var manager = await _context.Managers.AsNoTracking().FirstOrDefaultAsync(m => m.Id == request.ManagerId)
                          ?? throw new ArgumentException("Manager no existe.");

            if (manager.VenueId != request.VenueId)
                throw new ArgumentException("El Manager no pertenece al Venue indicado.");

            var venueExists = await _context.Venues.AsNoTracking().AnyAsync(v => v.Id == request.VenueId);
            if (!venueExists)
                throw new ArgumentException("Venue no existe.");

            var unitPrice = request.UnitPrice ?? CalculateTicketPrice(request.Type, request.Context);
            if (unitPrice <= 0)
                throw new ArgumentException("No se pudo calcular el precio del ticket. Indica UnitPrice o define lógica de precios.");

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

                    var item = new SaleItem
                    {
                        Ticket = ticket,
                        Quantity = 1,
                        UnitPrice = unitPrice
                    };

                    sale.Items.Add(item);
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

        public async Task<Sale> SellSubscriptionAsync(SellSubscriptionDTO request)
        {
            var manager = await _context.Managers.AsNoTracking().FirstOrDefaultAsync(m => m.Id == request.ManagerId)
                          ?? throw new ArgumentException("Manager no existe.");

            if (manager.VenueId != request.VenueId)
                throw new ArgumentException("El Manager no pertenece al Venue indicado.");

            var venueExists = await _context.Venues.AsNoTracking().AnyAsync(v => v.Id == request.VenueId);
            if (!venueExists)
                throw new ArgumentException("Venue no existe.");

            var clientExists = await _context.Clients.AsNoTracking().AnyAsync(c => c.Id == request.ClientId);
            if (!clientExists)
                throw new ArgumentException("Client no existe.");

            var price = request.Price > 0 ? request.Price : CalculateSubscriptionPrice(request.Category, request.Duration);
            if (price <= 0)
                throw new ArgumentException("No se pudo calcular el precio de la suscripción.");

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
                    Price = price,
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
                            UnitPrice = price
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

        private static decimal CalculateSubscriptionPrice(SubscriptionCategory category, SubscriptionDuration duration)
        {
            // Regla simple para no bloquear el flujo. Ideal: mover a PricePlan en BD.
            var baseMonthly = category switch
            {
                SubscriptionCategory.Niño => 15m,
                SubscriptionCategory.Adulto => 25m,
                SubscriptionCategory.Jubilado => 20m,
                SubscriptionCategory.FamiliaNumerosa => 30m,
                _ => 25m
            };

            return duration switch
            {
                SubscriptionDuration.Quincenal => Math.Round(baseMonthly * 0.6m, 2),
                SubscriptionDuration.Mensual => baseMonthly,
                SubscriptionDuration.Temporada => Math.Round(baseMonthly * 3.0m, 2),
                _ => baseMonthly
            };
        }

        private static decimal CalculateTicketPrice(TicketType type, TicketContext context)
        {
            // Si ya tienes una lógica real, sustitúyela aquí.
            // Por defecto, devuelve 0 para forzar a enviar UnitPrice desde UI si no quieres “inventar” precios.
            return 0m;
        }
    }
}
