using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

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

        // 🔹 Obtener todas las ventas (incluyendo items y sus relaciones)
        public async Task<ICollection<Sale>> GetAllAsync()
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Sale> cachedSales))
                return cachedSales;

            var sales = await _context.Sales
                .Include(s => s.Venue)
                .Include(s => s.Manager)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Ticket)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Subscription)
                .OrderByDescending(s => s.Date)
                .AsNoTracking()
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheExpirationTime));

            _cache.Set(_cacheKey, sales, cacheOptions);

            return sales;
        }

        // 🔹 Obtener venta específica (con Items)
        public async Task<Sale?> GetAsync(int id)
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Sale> cachedSales))
                return cachedSales.FirstOrDefault(s => s.Id == id);

            return await _context.Sales
                .Include(s => s.Venue)
                .Include(s => s.Manager)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Ticket)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Subscription)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Sales.AnyAsync(s => s.Id == id);

        public async Task<bool> CreateAsync(Sale sale)
        {
            await _context.Sales.AddAsync(sale);
            return await SaveAsync();
        }

        public async Task<bool> UpdateAsync(Sale sale)
        {
            _context.Update(sale);
            return await SaveAsync();
        }

        // 🔹 Eliminar venta
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
    }
}
