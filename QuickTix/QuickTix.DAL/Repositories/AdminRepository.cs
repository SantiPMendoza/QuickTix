using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Repositories
{
    /// <summary>
    /// Repositorio de acceso a datos para <see cref="Admin"/>.
    /// Incluye caché en lecturas de listado para reducir carga de consultas.
    /// </summary>
    public class AdminRepository : IAdminRepository
    {
        // Contexto EF Core de la aplicación.
        private readonly ApplicationDbContext _context;

        // Caché en memoria para acelerar lecturas de Admins.
        private readonly IMemoryCache _cache;

        // Clave de caché para la colección de administradores.
        private readonly string _adminCacheKey = "AdminCacheKey";

        // Duración de expiración de la caché (en segundos).
        private readonly int _cacheExpirationTime = 3600;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="AdminRepository"/>.
        /// </summary>
        /// <param name="context">DbContext de la aplicación.</param>
        /// <param name="cache">Caché en memoria.</param>
        public AdminRepository(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        /// <summary>
        /// Persiste los cambios en base de datos.
        /// Si se guarda correctamente, invalida la caché asociada.
        /// </summary>
        /// <returns>True si el guardado se realiza correctamente; en caso contrario, false.</returns>
        public async Task<bool> SaveAsync()
        {
            var result = await _context.SaveChangesAsync() >= 0;
            if (result)
                ClearCache();

            return result;
        }

        /// <summary>
        /// Invalida la caché de administradores.
        /// </summary>
        public void ClearCache()
        {
            _cache.Remove(_adminCacheKey);
        }

        /// <summary>
        /// Obtiene el listado de administradores.
        /// Usa caché para evitar consultas repetitivas.
        /// Se proyectan campos mínimos del <see cref="AppUser"/> asociado.
        /// </summary>
        /// <returns>Colección de administradores.</returns>
        public async Task<ICollection<Admin>> GetAllAsync()
        {
            if (_cache.TryGetValue(_adminCacheKey, out ICollection<Admin> cachedAdmins))
                return cachedAdmins;

            var admins = await _context.Admins
                .AsNoTracking()
                .Select(a => new Admin
                {
                    Id = a.Id,
                    Name = a.Name,
                    AppUserId = a.AppUserId,
                    AppUser = new AppUser
                    {
                        Email = a.AppUser.Email,
                        PhoneNumber = a.AppUser.PhoneNumber,
                        Nif = a.AppUser.Nif
                    }
                })
                .OrderBy(a => a.Id)
                .ToListAsync();

            _cache.Set(_adminCacheKey, admins, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheExpirationTime)));

            return admins;
        }

        /// <summary>
        /// Obtiene un administrador por id para lectura.
        /// Si existe caché de listado, se reutiliza para evitar consulta a BD.
        /// </summary>
        /// <param name="id">Identificador del administrador.</param>
        /// <returns>Administrador si existe; en caso contrario, null.</returns>
        public async Task<Admin?> GetAsync(int id)
        {
            if (_cache.TryGetValue(_adminCacheKey, out ICollection<Admin> cachedAdmins))
                return cachedAdmins.FirstOrDefault(a => a.Id == id);

            return await _context.Admins
                .AsNoTracking()
                .Include(a => a.AppUser)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        /// <summary>
        /// Obtiene un administrador por id para actualización/borrado.
        /// La entidad se devuelve con tracking y con el <see cref="AppUser"/> incluido.
        /// </summary>
        /// <param name="id">Identificador del administrador.</param>
        /// <returns>Administrador si existe; en caso contrario, null.</returns>
        public async Task<Admin?> GetForUpdateAsync(int id)
        {
            return await _context.Admins
                .Include(a => a.AppUser)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        /// <summary>
        /// Obtiene el detalle de un administrador.
        /// Si el "detalle" no requiere información adicional, reutiliza <see cref="GetAsync"/>.
        /// </summary>
        /// <param name="id">Identificador del administrador.</param>
        /// <returns>Administrador si existe; en caso contrario, null.</returns>
        public async Task<Admin?> GetDetailAsync(int id)
        {
            return await GetAsync(id);
        }

        /// <summary>
        /// Indica si existe un administrador con el id especificado.
        /// </summary>
        /// <param name="id">Identificador del administrador.</param>
        /// <returns>True si existe; en caso contrario, false.</returns>
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Admins.AnyAsync(a => a.Id == id);
        }

        /// <summary>
        /// Crea un administrador y persiste cambios.
        /// </summary>
        /// <param name="admin">Entidad administrador.</param>
        /// <returns>True si se guarda correctamente; en caso contrario, false.</returns>
        public async Task<bool> CreateAsync(Admin admin)
        {
            await _context.Admins.AddAsync(admin);
            return await SaveAsync();
        }

        /// <summary>
        /// Persiste una actualización de administrador.
        /// Convención habitual: la entidad se obtiene previamente con tracking mediante <see cref="GetForUpdateAsync"/>.
        /// </summary>
        /// <param name="admin">Entidad administrador.</param>
        /// <returns>True si se guarda correctamente; en caso contrario, false.</returns>
        public async Task<bool> UpdateAsync(Admin admin)
        {
            // Se asume entidad trackeada; si no lo estuviera, habría que adjuntarla/actualizarla explícitamente.
            return await SaveAsync();
        }

        /// <summary>
        /// Elimina un administrador por id y persiste cambios.
        /// </summary>
        /// <param name="id">Identificador del administrador.</param>
        /// <returns>True si se elimina; false si no existe.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            // Cambio por seguridad: evitar borrar una entidad proveniente de caché/proyección.
            var admin = await GetForUpdateAsync(id);
            if (admin == null)
                return false;

            _context.Admins.Remove(admin);
            return await SaveAsync();
        }
    }
}
