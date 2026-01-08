using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Repositories
{
    /// <summary>
    /// Repositorio de acceso a datos para <see cref="Venue"/>.
    /// Incluye caché para el listado general y expone operaciones CRUD básicas.
    /// </summary>
    public class VenueRepository : IVenueRepository
    {
        // Contexto EF Core de la aplicación
        private readonly ApplicationDbContext _context;

        // Caché en memoria para acelerar lecturas
        private readonly IMemoryCache _cache;

        // Clave de caché para el listado de recintos
        private readonly string _cacheKey = "VenueCacheKey";

        // Tiempo de expiración de la caché (en segundos)
        private readonly int _cacheExpirationTime = 3600;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="VenueRepository"/>.
        /// </summary>
        /// <param name="context">DbContext de la aplicación.</param>
        /// <param name="cache">Caché en memoria.</param>
        public VenueRepository(ApplicationDbContext context, IMemoryCache cache)
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
        /// Invalida la caché de recintos.
        /// </summary>
        public void ClearCache() => _cache.Remove(_cacheKey);

        /// <summary>
        /// Obtiene el listado general de recintos.
        /// Usa caché para evitar consultas repetitivas.
        /// Se proyectan únicamente los campos básicos del recinto.
        /// </summary>
        /// <returns>Colección de recintos.</returns>
        public async Task<ICollection<Venue>> GetAllAsync()
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Venue> cachedVenues))
                return cachedVenues;

            var venues = await _context.Venues
                .AsNoTracking()
                .Select(v => new Venue
                {
                    Id = v.Id,
                    Name = v.Name,
                    Location = v.Location,
                    Capacity = v.Capacity,
                    IsActive = v.IsActive
                })
                .OrderBy(v => v.Name)
                .ToListAsync();

            _cache.Set(_cacheKey, venues, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheExpirationTime)));

            return venues;
        }

        /// <summary>
        /// Obtiene un recinto por id para consulta.
        /// Si existe caché del listado, se reutiliza para evitar consulta a BD.
        /// </summary>
        /// <param name="id">Identificador del recinto.</param>
        /// <returns>Recinto si existe; en caso contrario, null.</returns>
        public async Task<Venue?> GetAsync(int id)
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Venue> cachedVenues))
                return cachedVenues.FirstOrDefault(v => v.Id == id);

            return await _context.Venues
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        /// <summary>
        /// Obtiene un recinto por id para actualización/borrado.
        /// Se devuelve entidad con tracking.
        /// </summary>
        /// <param name="id">Identificador del recinto.</param>
        /// <returns>Recinto si existe; en caso contrario, null.</returns>
        public async Task<Venue?> GetForUpdateAsync(int id)
        {
            return await _context.Venues
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        /// <summary>
        /// Obtiene el detalle completo de un recinto incluyendo relaciones principales.
        /// Se usa cuando una pantalla requiere el grafo completo (managers, tickets, abonos, ventas).
        /// Usa split query para evitar explosiones cartesianas en colecciones.
        /// </summary>
        /// <param name="id">Identificador del recinto.</param>
        /// <returns>Recinto con detalle si existe; en caso contrario, null.</returns>
        public async Task<Venue?> GetDetailAsync(int id)
        {
            return await _context.Venues
                .AsNoTracking()
                .Include(v => v.Managers)
                .Include(v => v.Tickets)
                .Include(v => v.Subscriptions)
                .Include(v => v.Sales)
                .AsSplitQuery()
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        /// <summary>
        /// Persiste una actualización de recinto.
        /// Convención habitual: la entidad se obtiene previamente con tracking mediante <see cref="GetForUpdateAsync"/>.
        /// </summary>
        /// <param name="venue">Entidad recinto.</param>
        /// <returns>True si se guarda correctamente; en caso contrario, false.</returns>
        public async Task<bool> UpdateAsync(Venue venue)
        {
            return await SaveAsync();
        }

        /// <summary>
        /// Indica si existe un recinto con el id especificado.
        /// </summary>
        /// <param name="id">Identificador del recinto.</param>
        /// <returns>True si existe; en caso contrario, false.</returns>
        public async Task<bool> ExistsAsync(int id) =>
            await _context.Venues.AnyAsync(v => v.Id == id);

        /// <summary>
        /// Crea un recinto y persiste cambios.
        /// </summary>
        /// <param name="venue">Entidad recinto.</param>
        /// <returns>True si se guarda correctamente; en caso contrario, false.</returns>
        public async Task<bool> CreateAsync(Venue venue)
        {
            await _context.Venues.AddAsync(venue);
            return await SaveAsync();
        }

        /// <summary>
        /// Elimina un recinto por id y persiste cambios.
        /// Para evitar problemas con entidades proyectadas desde caché, la eliminación se realiza
        /// sobre una entidad cargada con tracking.
        /// </summary>
        /// <param name="id">Identificador del recinto.</param>
        /// <returns>True si se elimina; false si no existe.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            var venue = await GetForUpdateAsync(id);
            if (venue == null) return false;

            _context.Venues.Remove(venue);
            return await SaveAsync();
        }
    }
}
