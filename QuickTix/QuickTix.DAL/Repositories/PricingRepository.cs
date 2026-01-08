using QuickTix.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities.Price;
using QuickTix.DAL.Data;
using System.Collections.Concurrent;

namespace QuickTix.DAL.Repositories
{
    public class PricingRepository : IPricingRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        private const int CacheSeconds = 3600;
        private const string CachePrefix = "Pricing:Venue:";

        // Mantiene qué venues han sido cacheados para poder limpiar sin enumerar la cache
        private static readonly ConcurrentDictionary<int, byte> CachedVenueIds = new();

        public PricingRepository(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public void ClearCache()
        {
            foreach (var venueId in CachedVenueIds.Keys)
            {
                _cache.Remove($"{CachePrefix}{venueId}");
            }

            CachedVenueIds.Clear();
        }

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

            var ticketDict = ticketsDb.ToDictionary(
                x => (x.Type, x.Context),
                x => x);

            var subsDict = subsDb.ToDictionary(
                x => (x.Category, x.Duration),
                x => x);

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

            // Registrar venue cacheado
            CachedVenueIds.TryAdd(venueId, 0);

            return map;
        }

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
