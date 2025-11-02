using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Repositories
{
    public class VenueRepository : IVenueRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly string _cacheKey = "VenueCacheKey";
        private readonly int _cacheExpirationTime = 3600;

        public VenueRepository(ApplicationDbContext context, IMemoryCache cache)
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

        public async Task<ICollection<Venue>> GetAllAsync()
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Venue> cachedVenues))
                return cachedVenues;

            var venues = await _context.Venues
                .Include(v => v.Managers)
                .Include(v => v.Tickets)
                .Include(v => v.Subscriptions)
                .Include(v => v.Sales)
                .OrderBy(v => v.Name)
                .ToListAsync();

            _cache.Set(_cacheKey, venues, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheExpirationTime)));

            return venues;
        }

        public async Task<Venue?> GetAsync(int id)
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Venue> cachedVenues))
                return cachedVenues.FirstOrDefault(v => v.Id == id);

            return await _context.Venues
                .Include(v => v.Managers)
                .Include(v => v.Tickets)
                .Include(v => v.Subscriptions)
                .Include(v => v.Sales)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Venues.AnyAsync(v => v.Id == id);

        public async Task<bool> CreateAsync(Venue venue)
        {
            await _context.Venues.AddAsync(venue);
            return await SaveAsync();
        }

        public async Task<bool> UpdateAsync(Venue venue)
        {
            _context.Update(venue);
            return await SaveAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var venue = await GetAsync(id);
            if (venue == null) return false;
            _context.Venues.Remove(venue);
            return await SaveAsync();
        }
    }
}
