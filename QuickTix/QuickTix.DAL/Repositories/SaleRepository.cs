using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.DTOs.SalesHistory;
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
            // Se asume que "sale" viene trackeado desde GetForUpdateAsync y ya tiene cambios aplicados.
            // Si lo pasas detached, no se persistirá nada.
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

        public async Task<IEnumerable<TicketSaleHistoryDTO>> GetTicketHistoryAsync()
        {
            return await _context.Sales
                .AsNoTracking()
                .SelectMany(s => s.Items
                    .Where(i => i.TicketId != null)
                    .Select(i => new TicketSaleHistoryDTO
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

        public async Task<IEnumerable<SubscriptionSaleHistoryDTO>> GetSubscriptionHistoryAsync()
        {
            var rows = await _context.Sales
                .AsNoTracking()
                .SelectMany(s => s.Items
                    .Where(i => i.SubscriptionId != null)
                    .Select(i => new
                    {
                        SaleId = s.Id,
                        s.Date,

                        s.VenueId,
                        VenueName = s.Venue.Name,

                        s.ManagerId,
                        ManagerName = s.Manager.Name,

                        Category = i.Subscription.Category,
                        Price = i.UnitPrice,

                        ClientName = i.Subscription.Client != null ? i.Subscription.Client.Name : string.Empty
                    }))
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            return rows.Select(x => new SubscriptionSaleHistoryDTO
            {
                Id = x.SaleId,
                Date = x.Date,

                VenueId = x.VenueId,
                VenueName = x.VenueName,

                ManagerId = x.ManagerId,
                ManagerName = x.ManagerName,

                SubscriptionCategory = x.Category.ToString(),
                Price = x.Price,

                ClientName = x.ClientName
            });
        }
    }
}
