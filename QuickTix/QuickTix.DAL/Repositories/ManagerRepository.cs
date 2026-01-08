using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Repositories
{
    /// <summary>
    /// Repositorio de acceso a datos para <see cref="Manager"/>.
    /// Incluye caché en lecturas de listado para reducir carga de consultas.
    ///
    /// </summary>
    public class ManagerRepository : IManagerRepository
    {
        // Contexto EF Core de la aplicación.
        private readonly ApplicationDbContext _context;

        // Caché en memoria para acelerar lecturas.
        private readonly IMemoryCache _cache;

        // Clave de caché para la colección de managers.
        private readonly string _cacheKey = "ManagerCacheKey";

        // Duración de expiración de la caché (en segundos).
        private readonly int _cacheExpirationTime = 3600;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="ManagerRepository"/>.
        /// </summary>
        /// <param name="context">DbContext de la aplicación.</param>
        /// <param name="cache">Caché en memoria.</param>
        public ManagerRepository(ApplicationDbContext context, IMemoryCache cache)
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
            if (result) ClearCache();
            return result;
        }

        /// <summary>
        /// Invalida la caché de managers.
        /// </summary>
        public void ClearCache() => _cache.Remove(_cacheKey);

        /// <summary>
        /// Obtiene el listado de managers.
        /// Usa caché para evitar consultas repetitivas.
        /// Se proyectan campos mínimos de <see cref="AppUser"/> y <see cref="Venue"/> asociados.
        /// </summary>
        /// <returns>Colección de managers.</returns>
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

        /// <summary>
        /// Obtiene un manager por id para lectura.
        /// Si existe caché del listado, se reutiliza para evitar consulta a BD.
        /// Si no hay caché, realiza una lectura ligera (no-tracking) incluyendo usuario y venue.
        /// </summary>
        /// <param name="id">Identificador del manager.</param>
        /// <returns>Manager si existe; en caso contrario, null.</returns>
        public async Task<Manager?> GetAsync(int id)
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Manager> cachedManagers))
                return cachedManagers.FirstOrDefault(m => m.Id == id);

            return await _context.Managers
                .AsNoTracking()
                .Include(m => m.AppUser)
                .Include(m => m.Venue)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        /// <summary>
        /// Obtiene un manager por id para actualización/borrado.
        /// La entidad se devuelve con tracking e incluye el <see cref="AppUser"/> asociado.
        /// No incluye colecciones para evitar carga innecesaria en operaciones de escritura.
        /// </summary>
        /// <param name="id">Identificador del manager.</param>
        /// <returns>Manager si existe; en caso contrario, null.</returns>
        public async Task<Manager?> GetForUpdateAsync(int id)
        {
            return await _context.Managers
                .Include(m => m.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        /// <summary>
        /// Obtiene el detalle de un manager.
        /// Actualmente reutiliza <see cref="GetAsync(int)"/> (incluye AppUser y Venue).
        /// </summary>
        /// <param name="id">Identificador del manager.</param>
        /// <returns>Manager si existe; en caso contrario, null.</returns>
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

        /// <summary>
        /// Indica si existe un manager con el id especificado.
        /// </summary>
        /// <param name="id">Identificador del manager.</param>
        /// <returns>True si existe; en caso contrario, false.</returns>
        public async Task<bool> ExistsAsync(int id) =>
            await _context.Managers.AnyAsync(m => m.Id == id);

        /// <summary>
        /// Crea un manager y persiste cambios.
        /// </summary>
        /// <param name="manager">Entidad manager.</param>
        /// <returns>True si se guarda correctamente; en caso contrario, false.</returns>
        public async Task<bool> CreateAsync(Manager manager)
        {
            await _context.Managers.AddAsync(manager);
            return await SaveAsync();
        }

        /// <summary>
        /// Persiste una actualización de manager.
        /// Convención: la entidad se obtiene previamente con tracking mediante <see cref="GetForUpdateAsync"/>.
        /// </summary>
        /// <param name="manager">Entidad manager.</param>
        /// <returns>True si se guarda correctamente; en caso contrario, false.</returns>
        public async Task<bool> UpdateAsync(Manager manager)
        {
            // Importante: evitar _context.Update(manager) para no propagar actualizaciones a través del grafo.
            return await SaveAsync();
        }

        /// <summary>
        /// Elimina un manager por id y persiste cambios.
        ///
        /// Además, elimina también el <see cref="AppUser"/> asociado si existe.
        /// </summary>
        /// <param name="id">Identificador del manager.</param>
        /// <returns>True si se elimina; false si no existe.</returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si el manager tiene ventas registradas (regla de integridad de dominio).
        /// </exception>
        public async Task<bool> DeleteAsync(int id)
        {
            // Escritura: se consulta siempre desde BD con tracking.
            // Se incluyen Sales y AppUser para validar la regla y eliminar el usuario asociado.
            var manager = await _context.Managers
                .Include(m => m.Sales)
                .Include(m => m.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (manager == null)
                return false;

            if (manager.Sales != null && manager.Sales.Any())
                throw new InvalidOperationException("No se puede eliminar un gestor con ventas registradas.");

            if (manager.AppUser != null)
                _context.AppUsers.Remove(manager.AppUser);

            _context.Managers.Remove(manager);
            return await SaveAsync();
        }
    }
}
