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
                .AsNoTracking()
                .Select(c => new Client
                {
                    Id = c.Id,
                    Name = c.Name,
                    AppUserId = c.AppUserId,
                    AppUser = new AppUser
                    {
                        Email = c.AppUser.Email,
                        PhoneNumber = c.AppUser.PhoneNumber,
                        Nif = c.AppUser.Nif
                    }
                })
                .OrderBy(c => c.Id)
                .ToListAsync();

            _cache.Set(_cacheKey, clients, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheExpirationTime)));

            return clients;
        }

        // Detalle para panel derecho (Abonos/Entradas)
        public async Task<Client?> GetDetailAsync(int id)
        {
            return await _context.Clients
                .AsNoTracking()
                .Include(c => c.AppUser)
                .Include(c => c.Subscriptions)
                .Include(c => c.Tickets)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // Exclusivo para escritura (tracking, sin caché, sin colecciones)
        public async Task<Client?> GetForUpdateAsync(int id)
        {
            return await _context.Clients
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Client?> GetAsync(int id)
        {
            // Consistente: GetAsync puede resolverse desde el listado cacheado
            if (_cache.TryGetValue(_cacheKey, out ICollection<Client> cachedClients))
                return cachedClients.FirstOrDefault(c => c.Id == id);

            // Fallback ligero (sin colecciones)
            return await _context.Clients
                .AsNoTracking()
                .Include(c => c.AppUser)
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
            // _context.Clients.Update(client);
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
