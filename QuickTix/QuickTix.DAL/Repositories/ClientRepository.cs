using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly string _cacheKey = "ClientCacheKey";
        private readonly int _cacheExpirationTime = 3600;

        public ClientRepository(ApplicationDbContext context, IMemoryCache cache)
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

        public async Task<ICollection<Client>> GetAllAsync()
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Client> cachedClients))
                return cachedClients;

            var clients = await _context.Clients
                .Include(c => c.AppUser)
                .Include(c => c.Subscriptions)
                .Include(c => c.Tickets)
                .OrderBy(c => c.Id)
                .ToListAsync();

            _cache.Set(_cacheKey, clients, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheExpirationTime)));

            return clients;
        }

        public async Task<Client?> GetAsync(int id)
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Client> cachedClients))
                return cachedClients.FirstOrDefault(c => c.Id == id);

            return await _context.Clients
                .Include(c => c.AppUser)
                .Include(c => c.Subscriptions)
                .Include(c => c.Tickets)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Clients.AnyAsync(c => c.Id == id);

        public async Task<bool> CreateAsync(Client client)
        {
            await _context.Clients.AddAsync(client);
            return await SaveAsync();
        }

        public async Task<bool> UpdateAsync(Client client)
        {
            _context.Update(client);
            return await SaveAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var client = await GetAsync(id);
            if (client == null) return false;
            _context.Clients.Remove(client);
            return await SaveAsync();
        }
    }
}
