using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Repositories
{
    public class ManagerRepository : IManagerRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly string _cacheKey = "ManagerCacheKey";
        private readonly int _cacheExpirationTime = 3600;

        public ManagerRepository(ApplicationDbContext context, IMemoryCache cache)
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

        public async Task<ICollection<Manager>> GetAllAsync()
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Manager> cachedManagers))
                return cachedManagers;

            var managers = await _context.Managers
                .AsNoTracking()
                .Select(m => new Manager
                {
                    Id = m.Id,
                    Name = m.Name,
                    VenueId = m.VenueId,
                    AppUserId = m.AppUserId,

                    AppUser = new AppUser
                    {
                        Email = m.AppUser.Email,
                        PhoneNumber = m.AppUser.PhoneNumber,
                        Nif = m.AppUser.Nif
                    },

                    Venue = new Venue
                    {
                        Id = m.Venue.Id,
                        Name = m.Venue.Name
                    }
                })
                .OrderBy(m => m.Id)
                .ToListAsync();

            _cache.Set(_cacheKey, managers, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheExpirationTime)));

            return managers;
        }

        public async Task<Manager?> GetAsync(int id)
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Manager> cachedManagers))
                return cachedManagers.FirstOrDefault(m => m.Id == id);

            // Fallback ligero, coherente con patrón (lectura, no tracking, sin colecciones)
            return await _context.Managers
                .AsNoTracking()
                .Include(m => m.AppUser)
                .Include(m => m.Venue)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<Manager?> GetForUpdateAsync(int id)
        {
            // Escritura: tracking, sin caché, solo lo necesario
            return await _context.Managers
                .Include(m => m.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
        }


        public async Task<Manager?> GetDetailAsync(int id)
        {
            return await GetAsync(id);

            // Alternativa sin caché:
            // return await _context.Managers
            //     .AsNoTracking()
            //     .Include(m => m.AppUser)
            //     .Include(m => m.Venue)
            //     .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Managers.AnyAsync(m => m.Id == id);

        public async Task<bool> CreateAsync(Manager manager)
        {
            await _context.Managers.AddAsync(manager);
            return await SaveAsync();
        }

        public async Task<bool> UpdateAsync(Manager manager)
        {
            // Importante: no usar _context.Update(manager) para evitar updates por grafo
            return await SaveAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var manager = await _context.Managers
                .Include(m => m.Sales)
                .Include(m => m.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (manager == null)
                return false;

            if (manager.Sales != null && manager.Sales.Any())
                throw new InvalidOperationException("No se puede eliminar un gestor con ventas registradas.");

            // Si quieres eliminar también su AppUser
            if (manager.AppUser != null)
                _context.AppUsers.Remove(manager.AppUser);

            _context.Managers.Remove(manager);
            return await SaveAsync();
        }
    }
}
