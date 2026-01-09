using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Repositories
{
    /// <summary>
    /// Repositorio de solo lectura para <see cref="SaleItem"/>.
    /// 
    /// Este repositorio está orientado a consultas ricas (con múltiples Includes)
    /// y no expone operaciones de creación, actualización o borrado, ya que
    /// <see cref="SaleItem"/> forma parte del agregado de una venta (<see cref="Sale"/>).
    /// </summary>
    public class SaleItemRepository : ISaleItemRepository
    {
        // Contexto EF Core de la aplicación
        private readonly ApplicationDbContext _context;

        // Caché en memoria para acelerar lecturas de ítems de venta
        private readonly IMemoryCache _cache;

        // Clave de caché para el listado completo de SaleItems
        private readonly string _cacheKey = "SaleItemCacheKey";

        // Tiempo de expiración de la caché (en segundos)
        private readonly int _cacheExpirationTime = 3600;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="SaleItemRepository"/>.
        /// </summary>
        /// <param name="context">DbContext de la aplicación.</param>
        /// <param name="cache">Caché en memoria.</param>
        public SaleItemRepository(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        /// <summary>
        /// Invalida la caché de ítems de venta.
        /// Uso interno; este repositorio no expone operaciones de escritura.
        /// </summary>
        private void ClearCache() => _cache.Remove(_cacheKey);

        /// <summary>
        /// Consulta base reutilizable para <see cref="SaleItem"/>.
        /// Incluye todas las relaciones necesarias para vistas de consulta:
        /// - Venta, gestor y recinto
        /// - Ticket y su recinto
        /// - Suscripción, su recinto y cliente
        /// 
        /// Se ejecuta siempre en modo no-tracking al ser un repositorio de lectura.
        /// </summary>
        /// <returns>Consulta IQueryable de <see cref="SaleItem"/>.</returns>
        private IQueryable<SaleItem> BaseQuery() =>
            _context.SaleItems
                .Include(i => i.Sale)
                    .ThenInclude(s => s.Manager)
                .Include(i => i.Sale)
                    .ThenInclude(s => s.Venue)
                .Include(i => i.Ticket)
                    .ThenInclude(t => t.Venue)
                .Include(i => i.Subscription)
                    .ThenInclude(s => s.Venue)
                .Include(i => i.Subscription)
                    .ThenInclude(s => s.Client)
                .AsNoTracking();

        /// <summary>
        /// Obtiene el listado completo de ítems de venta (tickets y suscripciones).
        /// El resultado se cachea para mejorar el rendimiento en listados frecuentes.
        /// </summary>
        /// <returns>Colección de ítems de venta.</returns>
        public async Task<ICollection<SaleItem>> GetAllAsync()
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<SaleItem> cachedItems))
                return cachedItems;

            var saleItems = await BaseQuery()
                .OrderByDescending(i => i.Sale.Date)
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheExpirationTime));

            _cache.Set(_cacheKey, saleItems, cacheOptions);

            return saleItems;
        }

        /// <summary>
        /// Obtiene un ítem de venta por su identificador.
        /// Primero intenta resolverlo desde caché; si no existe, consulta base de datos.
        /// </summary>
        /// <param name="id">Identificador del ítem de venta.</param>
        /// <returns>Ítem de venta si existe; en caso contrario, null.</returns>
        public async Task<SaleItem?> GetAsync(int id)
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<SaleItem> cachedItems))
            {
                var item = cachedItems.FirstOrDefault(i => i.Id == id);
                if (item != null)
                    return item;
            }

            return await BaseQuery()
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        /// <summary>
        /// Indica si existe un ítem de venta con el identificador especificado.
        /// </summary>
        /// <param name="id">Identificador del ítem de venta.</param>
        /// <returns>True si existe; en caso contrario, false.</returns>
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.SaleItems
                .AnyAsync(i => i.Id == id);
        }

        /// <summary>
        /// Obtiene únicamente los ítems de venta correspondientes a tickets.
        /// </summary>
        /// <returns>Colección de ítems de venta de tipo ticket.</returns>
        public async Task<ICollection<SaleItem>> GetTicketsAsync()
        {
            return await BaseQuery()
                .Where(i => i.TicketId != null)
                .OrderByDescending(i => i.Sale.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene únicamente los ítems de venta correspondientes a suscripciones.
        /// </summary>
        /// <returns>Colección de ítems de venta de tipo suscripción.</returns>
        public async Task<ICollection<SaleItem>> GetSubscriptionsAsync()
        {
            return await BaseQuery()
                .Where(i => i.SubscriptionId != null)
                .OrderByDescending(i => i.Sale.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene los ítems de venta asociados a una venta concreta.
        /// </summary>
        /// <param name="saleId">Identificador de la venta.</param>
        /// <returns>Colección de ítems asociados a la venta.</returns>
        public async Task<ICollection<SaleItem>> GetBySaleAsync(int saleId)
        {
            return await BaseQuery()
                .Where(i => i.SaleId == saleId)
                .OrderByDescending(i => i.Sale.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Counts the by subscription asynchronous.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <returns></returns>
        public Task<int> CountBySubscriptionAsync(int subscriptionId)
            => _context.SaleItems.CountAsync(i => i.SubscriptionId == subscriptionId);

        /// <summary>
        /// Counts the by ticket asynchronous.
        /// </summary>
        /// <param name="ticketId">The ticket identifier.</param>
        /// <returns></returns>
        public Task<int> CountByTicketAsync(int ticketId)
            => _context.SaleItems.CountAsync(i => i.TicketId == ticketId);
    }
}
