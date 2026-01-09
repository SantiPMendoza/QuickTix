using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Repositories
{
    /// <summary>
    /// Repositorio de acceso a datos para <see cref="Subscription"/>.
    /// Incluye caché para el listado general y expone operaciones CRUD básicas,
    /// además de consultas filtradas por cliente.
    /// </summary>
    public class SubscriptionRepository : ISubscriptionRepository
    {
        // Contexto EF Core de la aplicación
        private readonly ApplicationDbContext _context;

        // Caché en memoria para acelerar lecturas
        private readonly IMemoryCache _cache;

        // Clave de caché para el listado de suscripciones
        private readonly string _cacheKey = "SubscriptionCacheKey";

        // Tiempo de expiración de la caché (en segundos)
        private readonly int _cacheExpirationTime = 3600;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="SubscriptionRepository"/>.
        /// </summary>
        /// <param name="context">DbContext de la aplicación.</param>
        /// <param name="cache">Caché en memoria.</param>
        public SubscriptionRepository(ApplicationDbContext context, IMemoryCache cache)
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
        /// Invalida la caché de suscripciones.
        /// </summary>
        public void ClearCache() => _cache.Remove(_cacheKey);

        /// <summary>
        /// Obtiene el listado general de suscripciones.
        /// Usa caché para evitar consultas repetitivas.
        /// Se proyectan datos básicos y referencias mínimas de <see cref="Venue"/> y <see cref="Client"/>.
        /// </summary>
        /// <returns>Colección de suscripciones.</returns>
        public async Task<ICollection<Subscription>> GetAllAsync()
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Subscription> cachedSubs))
                return cachedSubs;

            var subs = await _context.Subscriptions
                .AsNoTracking()
                .Select(s => new Subscription
                {
                    Id = s.Id,
                    Category = s.Category,
                    Duration = s.Duration,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Price = s.Price,
                    VenueId = s.VenueId,
                    ClientId = s.ClientId,

                    Venue = new Venue
                    {
                        Id = s.Venue.Id,
                        Name = s.Venue.Name
                    },

                    Client = new Client
                    {
                        Id = s.Client.Id,
                        Name = s.Client.Name
                    }
                })
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();

            _cache.Set(_cacheKey, subs, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheExpirationTime)));

            return subs;
        }

        /// <summary>
        /// Obtiene una suscripción por id para consulta.
        /// Si existe caché del listado, se reutiliza para evitar consulta a BD.
        /// </summary>
        /// <param name="id">Identificador de la suscripción.</param>
        /// <returns>Suscripción si existe; en caso contrario, null.</returns>
        public async Task<Subscription?> GetAsync(int id)
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Subscription> cachedSubs))
                return cachedSubs.FirstOrDefault(s => s.Id == id);

            return await _context.Subscriptions
                .AsNoTracking()
                .Include(s => s.Venue)
                .Include(s => s.Client)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        /// <summary>
        /// Obtiene una suscripción por id para actualización/borrado.
        /// Se devuelve entidad con tracking.
        /// </summary>
        /// <param name="id">Identificador de la suscripción.</param>
        /// <returns>Suscripción si existe; en caso contrario, null.</returns>
        public async Task<Subscription?> GetForUpdateAsync(int id)
        {
            return await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        /// <summary>
        /// Obtiene el detalle de una suscripción incluyendo sus relaciones principales.
        /// Se usa cuando alguna vista requiere el grafo completo.
        /// </summary>
        /// <param name="id">Identificador de la suscripción.</param>
        /// <returns>Suscripción con detalle si existe; en caso contrario, null.</returns>
        public async Task<Subscription?> GetDetailAsync(int id)
        {
            return await _context.Subscriptions
                .AsNoTracking()
                .Include(s => s.Venue)
                .Include(s => s.Client)
                .AsSplitQuery()
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        /// <summary>
        /// Persiste una actualización de suscripción.
        /// Convención habitual: la entidad se obtiene previamente con tracking mediante <see cref="GetForUpdateAsync"/>.
        /// </summary>
        /// <param name="subscription">Entidad suscripción.</param>
        /// <returns>True si se guarda correctamente; en caso contrario, false.</returns>
        public async Task<bool> UpdateAsync(Subscription subscription)
        {
            return await SaveAsync();
        }

        /// <summary>
        /// Indica si existe una suscripción con el id especificado.
        /// </summary>
        /// <param name="id">Identificador de la suscripción.</param>
        /// <returns>True si existe; en caso contrario, false.</returns>
        public async Task<bool> ExistsAsync(int id) =>
            await _context.Subscriptions.AnyAsync(s => s.Id == id);

        /// <summary>
        /// Crea una suscripción y persiste cambios.
        /// </summary>
        /// <param name="subscription">Entidad suscripción.</param>
        /// <returns>True si se guarda correctamente; en caso contrario, false.</returns>
        public async Task<bool> CreateAsync(Subscription subscription)
        {
            await _context.Subscriptions.AddAsync(subscription);
            return await SaveAsync();
        }

        /// <summary>
        /// Elimina una suscripción por id y persiste cambios.
        /// Para evitar problemas con entidades proyectadas desde caché, la eliminación se realiza
        /// sobre una entidad cargada con tracking. Si la suscripción tiene elementos de venta asociados,
        /// se eliminan también para que no queden huérfanos.
        /// </summary>
        /// <param name="id">Identificador de la suscripción.</param>
        /// <returns>True si se elimina; false si no existe.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            var sub = await GetForUpdateAsync(id);
            if (sub == null) return false;

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var items = await _context.SaleItems
                    .Where(i => i.SubscriptionId == id)
                    .ToListAsync();

                if (items.Count > 0)
                    _context.SaleItems.RemoveRange(items);

                _context.Subscriptions.Remove(sub);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                ClearCache();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }



        /// <summary>
        /// Obtiene las suscripciones asociadas a un cliente.
        /// Proyecta datos básicos e incluye información mínima del recinto (<see cref="Venue"/>).
        /// </summary>
        /// <param name="clientId">Identificador del cliente.</param>
        /// <returns>Colección de suscripciones del cliente.</returns>
        public async Task<ICollection<Subscription>> GetByClientAsync(int clientId)
        {
            return await _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.ClientId == clientId)
                .Select(s => new Subscription
                {
                    Id = s.Id,
                    Category = s.Category,
                    Duration = s.Duration,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Price = s.Price,
                    VenueId = s.VenueId,
                    ClientId = s.ClientId,

                    Venue = new Venue
                    {
                        Id = s.Venue.Id,
                        Name = s.Venue.Name
                    }
                })
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();
        }
    }
}
