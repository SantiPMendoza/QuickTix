using QuickTix.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities.Price;
using QuickTix.DAL.Data;
using System.Collections.Concurrent;

namespace QuickTix.DAL.Repositories
{
    /// <summary>
    /// Repositorio de precios por venue.
    /// Expone operaciones para obtener y actualizar el mapa completo de precios
    /// (tickets y suscripciones) combinando valores persistidos y combinaciones faltantes
    /// con precio por defecto.
    /// </summary>
    public class PricingRepository : IPricingRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        private const int CacheSeconds = 3600;
        private const string CachePrefix = "Pricing:Venue:";

        /// <summary>
        /// Registra qué venues han sido cacheados para poder invalidarlos
        /// sin enumerar todas las claves del <see cref="IMemoryCache"/>.
        /// </summary>
        private static readonly ConcurrentDictionary<int, byte> CachedVenueIds = new();

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="PricingRepository"/>.
        /// </summary>
        /// <param name="db">DbContext de la aplicación.</param>
        /// <param name="cache">Caché en memoria.</param>
        public PricingRepository(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        /// <summary>
        /// Invalida la caché de todos los venues que este repositorio haya cacheado.
        /// </summary>
        public void ClearCache()
        {
            // Ajuste mínimo: evitar enumerar Keys mientras se modifica el diccionario.
            // Tomar snapshot de claves para evitar problemas de concurrencia.
            var venueIds = CachedVenueIds.Keys.ToArray();

            foreach (var venueId in venueIds)
            {
                _cache.Remove($"{CachePrefix}{venueId}");
            }

            CachedVenueIds.Clear();
        }

        /// <summary>
        /// Obtiene el mapa de precios del venue indicado.
        /// Si faltan combinaciones en base de datos, se completan con precio 0.
        /// El resultado se cachea por venue.
        /// </summary>
        /// <param name="venueId">Identificador del venue.</param>
        /// <returns>Mapa de precios del venue.</returns>
        /// <exception cref="ArgumentException">Si <paramref name="venueId"/> es inválido.</exception>
        public async Task<VenuePriceMap> GetVenuePriceMapAsync(int venueId)
        {
            if (venueId <= 0)
                throw new ArgumentException("VenueId inválido.", nameof(venueId));

            var cacheKey = $"{CachePrefix}{venueId}";

            if (_cache.TryGetValue(cacheKey, out VenuePriceMap cached))
                return cached;

            var ticketsDb = await _db.VenueTicketPrices
                .AsNoTracking()
                .Where(x => x.VenueId == venueId)
                .ToListAsync();

            var subsDb = await _db.VenueSubscriptionPrices
                .AsNoTracking()
                .Where(x => x.VenueId == venueId)
                .ToListAsync();

            // ToDictionary lanza si hay claves duplicadas.
            // Si por datos inconsistentes existieran duplicados, preferimos quedarnos con el más reciente.
            var ticketDict = ticketsDb
                .GroupBy(x => (x.Type, x.Context))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(v => v.UpdatedAtUtc).First());

            var subsDict = subsDb
                .GroupBy(x => (x.Category, x.Duration))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(v => v.UpdatedAtUtc).First());

            var allTicketTypes = Enum.GetValues<TicketType>();
            var allTicketContexts = Enum.GetValues<TicketContext>();
            var allSubCategories = Enum.GetValues<SubscriptionCategory>();
            var allSubDurations = Enum.GetValues<SubscriptionDuration>();

            var tickets = new List<VenueTicketPrice>();
            foreach (var type in allTicketTypes)
            {
                foreach (var context in allTicketContexts)
                {
                    if (ticketDict.TryGetValue((type, context), out var existing))
                    {
                        tickets.Add(existing);
                    }
                    else
                    {
                        tickets.Add(new VenueTicketPrice
                        {
                            VenueId = venueId,
                            Type = type,
                            Context = context,
                            Price = 0m,
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }
                }
            }

            var subs = new List<VenueSubscriptionPrice>();
            foreach (var category in allSubCategories)
            {
                foreach (var duration in allSubDurations)
                {
                    if (subsDict.TryGetValue((category, duration), out var existing))
                    {
                        subs.Add(existing);
                    }
                    else
                    {
                        subs.Add(new VenueSubscriptionPrice
                        {
                            VenueId = venueId,
                            Category = category,
                            Duration = duration,
                            Price = 0m,
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }
                }
            }

            var map = new VenuePriceMap
            {
                VenueId = venueId,
                TicketPrices = tickets,
                SubscriptionPrices = subs
            };

            _cache.Set(cacheKey, map, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(CacheSeconds)));

            CachedVenueIds.TryAdd(venueId, 0);

            return map;
        }

        /// <summary>
        /// Inserta o actualiza el mapa completo de precios para un venue.
        /// Estrategia: reemplazo total (remove + add) dentro de transacción.
        /// Tras persistir, invalida caché del venue y devuelve el mapa recalculado.
        /// </summary>
        /// <param name="map">Mapa de precios a guardar.</param>
        /// <returns>Mapa de precios persistido y completado.</returns>
        /// <exception cref="ArgumentNullException">Si <paramref name="map"/> es null.</exception>
        /// <exception cref="ArgumentException">Si el VenueId es inválido.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Si alguno de los precios es negativo.</exception>
        public async Task<VenuePriceMap> UpsertVenuePriceMapAsync(VenuePriceMap map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            if (map.VenueId <= 0)
                throw new ArgumentException("VenueId inválido.", nameof(map.VenueId));

            var venueId = map.VenueId;

            var tickets = map.TicketPrices ?? new List<VenueTicketPrice>();
            var subs = map.SubscriptionPrices ?? new List<VenueSubscriptionPrice>();

            if (tickets.Any(x => x.Price < 0) || subs.Any(x => x.Price < 0))
                throw new ArgumentOutOfRangeException(nameof(map), "El precio no puede ser negativo.");

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var existingTickets = await _db.VenueTicketPrices
                    .Where(x => x.VenueId == venueId)
                    .ToListAsync();

                _db.VenueTicketPrices.RemoveRange(existingTickets);

                await _db.VenueTicketPrices.AddRangeAsync(tickets.Select(x => new VenueTicketPrice
                {
                    VenueId = venueId,
                    Type = x.Type,
                    Context = x.Context,
                    Price = x.Price,
                    UpdatedAtUtc = DateTime.UtcNow
                }));

                var existingSubs = await _db.VenueSubscriptionPrices
                    .Where(x => x.VenueId == venueId)
                    .ToListAsync();

                _db.VenueSubscriptionPrices.RemoveRange(existingSubs);

                await _db.VenueSubscriptionPrices.AddRangeAsync(subs.Select(x => new VenueSubscriptionPrice
                {
                    VenueId = venueId,
                    Category = x.Category,
                    Duration = x.Duration,
                    Price = x.Price,
                    UpdatedAtUtc = DateTime.UtcNow
                }));

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // Invalida caché del venue para servir el dato persistido.
                _cache.Remove($"{CachePrefix}{venueId}");
                CachedVenueIds.TryAdd(venueId, 0);

                return await GetVenuePriceMapAsync(venueId);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
