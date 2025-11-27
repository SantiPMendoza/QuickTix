using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly string _adminCacheKey = "AdminCacheKey";
        private readonly int _cacheExpirationTime = 3600; // seconds

        public AdminRepository(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<bool> SaveAsync()
        {
            var result = await _context.SaveChangesAsync() >= 0;
            if (result)
                ClearCache();
            return result;
        }

        public void ClearCache()
        {
            _cache.Remove(_adminCacheKey);
        }

        public async Task<ICollection<Admin>> GetAllAsync()
        {
            return await _context.Admins
                .AsNoTracking()
                .Select(a => new Admin
                {
                    Id = a.Id,
                    Name = a.Name,
                    AppUser = new AppUser
                    {
                        Email = a.AppUser.Email,
                        PhoneNumber = a.AppUser.PhoneNumber,
                        Nif = a.AppUser.Nif
                    }
                })
                .OrderBy(a => a.Id)
                .ToListAsync();
        }


        public async Task<Admin?> GetAsync(int id)
        {
            if (_cache.TryGetValue(_adminCacheKey, out ICollection<Admin> cachedAdmins))
            {
                var admin = cachedAdmins.FirstOrDefault(a => a.Id == id);
                if (admin != null)
                    return admin;
            }

            return await _context.Admins
                .Include(a => a.AppUser)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Admins.AnyAsync(a => a.Id == id);
        }

        public async Task<bool> CreateAsync(Admin admin)
        {
            await _context.Admins.AddAsync(admin);
            return await SaveAsync();
        }

        public async Task<bool> UpdateAsync(Admin admin)
        {
            //_context.Update(admin);
            return await SaveAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var admin = await GetAsync(id);
            if (admin == null)
                return false;

            _context.Admins.Remove(admin);
            return await SaveAsync();
        }
    }
}
