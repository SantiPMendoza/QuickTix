using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Repositories
{
    public class SaleItemRepository : ISaleItemRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly string _cacheKey = "SaleItemCacheKey";
        private readonly int _cacheExpirationTime = 3600; // seconds

        public SaleItemRepository(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        private void ClearCache() => _cache.Remove(_cacheKey);

        // 🔹 Carga base (para evitar repetir Includes)
        private IQueryable<SaleItem> BaseQuery() =>
            _context.SaleItems
                .Include(i => i.Sale)
                    .ThenInclude(s => s.Manager)
                .Include(i => i.Sale)
                    .ThenInclude(s => s.Venue)
                .Include(i => i.Ticket)
                    .ThenInclude(t => t.Venue)
                .Include(i => i.Subscription)
                    .ThenInclude(s => s.Venue)
                .Include(i => i.Subscription)
                    .ThenInclude(s => s.Client)
                .AsNoTracking();

        // 🔹 Obtener todos los ítems (tickets + suscripciones)
        public async Task<ICollection<SaleItem>> GetAllAsync()
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<SaleItem> cachedItems))
                return cachedItems;

            var saleItems = await BaseQuery()
                .OrderByDescending(i => i.Sale.Date)
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheExpirationTime));

            _cache.Set(_cacheKey, saleItems, cacheOptions);
            return saleItems;
        }

        // 🔹 Obtener un ítem por Id
        public async Task<SaleItem?> GetAsync(int id)
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<SaleItem> cachedItems))
            {
                var item = cachedItems.FirstOrDefault(i => i.Id == id);
                if (item != null)
                    return item;
            }

            return await BaseQuery().FirstOrDefaultAsync(i => i.Id == id);
        }

        // 🔹 Comprobar existencia
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.SaleItems.AnyAsync(i => i.Id == id);
        }

        // 🔹 Obtener solo los ítems de tipo Ticket
        public async Task<ICollection<SaleItem>> GetTicketsAsync()
        {
            return await BaseQuery()
                .Where(i => i.TicketId != null)
                .OrderByDescending(i => i.Sale.Date)
                .ToListAsync();
        }

        // 🔹 Obtener solo los ítems de tipo Subscription
        public async Task<ICollection<SaleItem>> GetSubscriptionsAsync()
        {
            return await BaseQuery()
                .Where(i => i.SubscriptionId != null)
                .OrderByDescending(i => i.Sale.Date)
                .ToListAsync();
        }

        // 🔹 Obtener ítems de una venta específica
        public async Task<ICollection<SaleItem>> GetBySaleAsync(int saleId)
        {
            return await BaseQuery()
                .Where(i => i.SaleId == saleId)
                .OrderByDescending(i => i.Sale.Date)
                .ToListAsync();
        }
    }
}
