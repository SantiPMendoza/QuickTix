using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly string _cacheKey = "SubscriptionCacheKey";
        private readonly int _cacheExpirationTime = 3600;

        public SubscriptionRepository(ApplicationDbContext context, IMemoryCache cache)
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

        public async Task<ICollection<Subscription>> GetAllAsync()
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Subscription> cachedSubs))
                return cachedSubs;

            var subs = await _context.Subscriptions
                .Include(s => s.Venue)
                .Include(s => s.Client)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();

            _cache.Set(_cacheKey, subs, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheExpirationTime)));

            return subs;
        }

        public async Task<Subscription?> GetAsync(int id)
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Subscription> cachedSubs))
                return cachedSubs.FirstOrDefault(s => s.Id == id);

            return await _context.Subscriptions
                .Include(s => s.Venue)
                .Include(s => s.Client)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Subscriptions.AnyAsync(s => s.Id == id);

        public async Task<bool> CreateAsync(Subscription subscription)
        {
            await _context.Subscriptions.AddAsync(subscription);
            return await SaveAsync();
        }

        public async Task<bool> UpdateAsync(Subscription subscription)
        {
            _context.Update(subscription);
            return await SaveAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var sub = await GetAsync(id);
            if (sub == null) return false;
            _context.Subscriptions.Remove(sub);
            return await SaveAsync();
        }
    }
}
