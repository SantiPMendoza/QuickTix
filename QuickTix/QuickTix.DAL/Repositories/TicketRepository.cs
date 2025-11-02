using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Repositories
{
    public class TicketRepository : ITicketRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly string _cacheKey = "TicketCacheKey";
        private readonly int _cacheExpirationTime = 3600;

        public TicketRepository(ApplicationDbContext context, IMemoryCache cache)
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

        public async Task<ICollection<Ticket>> GetAllAsync()
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Ticket> cachedTickets))
                return cachedTickets;

            var tickets = await _context.Tickets
                .Include(t => t.Venue)
                .Include(t => t.Client)
                .OrderByDescending(t => t.PurchaseDate)
                .ToListAsync();

            _cache.Set(_cacheKey, tickets, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheExpirationTime)));

            return tickets;
        }

        public async Task<Ticket?> GetAsync(int id)
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Ticket> cachedTickets))
                return cachedTickets.FirstOrDefault(t => t.Id == id);

            return await _context.Tickets
                .Include(t => t.Venue)
                .Include(t => t.Client)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Tickets.AnyAsync(t => t.Id == id);

        public async Task<bool> CreateAsync(Ticket ticket)
        {
            await _context.Tickets.AddAsync(ticket);
            return await SaveAsync();
        }

        public async Task<bool> UpdateAsync(Ticket ticket)
        {
            _context.Update(ticket);
            return await SaveAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var ticket = await GetAsync(id);
            if (ticket == null) return false;
            _context.Tickets.Remove(ticket);
            return await SaveAsync();
        }
    }
}
