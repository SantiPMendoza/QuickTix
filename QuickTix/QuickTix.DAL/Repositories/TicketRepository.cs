using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Repositories
{
    /// <summary>
    /// Repositorio de acceso a datos para <see cref="Ticket"/>.
    /// Incluye caché para el listado general y expone operaciones CRUD básicas.
    /// </summary>
    public class TicketRepository : ITicketRepository
    {
        // Contexto EF Core de la aplicación
        private readonly ApplicationDbContext _context;

        // Caché en memoria para acelerar lecturas
        private readonly IMemoryCache _cache;

        // Clave de caché para el listado de tickets
        private readonly string _cacheKey = "TicketCacheKey";

        // Tiempo de expiración de la caché (en segundos)
        private readonly int _cacheExpirationTime = 3600;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="TicketRepository"/>.
        /// </summary>
        /// <param name="context">DbContext de la aplicación.</param>
        /// <param name="cache">Caché en memoria.</param>
        public TicketRepository(ApplicationDbContext context, IMemoryCache cache)
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
        /// Invalida la caché de tickets.
        /// </summary>
        public void ClearCache() => _cache.Remove(_cacheKey);

        /// <summary>
        /// Obtiene el listado general de tickets.
        /// Usa caché para evitar consultas repetitivas.
        /// Se proyectan datos básicos y referencias mínimas de <see cref="Venue"/> y <see cref="Client"/>.
        /// </summary>
        /// <returns>Colección de tickets.</returns>
        public async Task<ICollection<Ticket>> GetAllAsync()
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Ticket> cachedTickets))
                return cachedTickets;

            var tickets = await _context.Tickets
                .AsNoTracking()
                .Select(t => new Ticket
                {
                    Id = t.Id,
                    Type = t.Type,
                    Context = t.Context,
                    Price = t.Price,
                    PurchaseDate = t.PurchaseDate,
                    VenueId = t.VenueId,
                    ClientId = t.ClientId,

                    Venue = new Venue
                    {
                        Id = t.Venue.Id,
                        Name = t.Venue.Name
                    },

                    Client = new Client
                    {
                        Id = t.Client.Id,
                        Name = t.Client.Name
                    }
                })
                .OrderByDescending(t => t.PurchaseDate)
                .ToListAsync();

            _cache.Set(_cacheKey, tickets, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheExpirationTime)));

            return tickets;
        }

        /// <summary>
        /// Obtiene un ticket por id para consulta.
        /// Si existe caché del listado, se reutiliza para evitar consulta a BD.
        /// </summary>
        /// <param name="id">Identificador del ticket.</param>
        /// <returns>Ticket si existe; en caso contrario, null.</returns>
        public async Task<Ticket?> GetAsync(int id)
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Ticket> cachedTickets))
                return cachedTickets.FirstOrDefault(t => t.Id == id);

            return await _context.Tickets
                .AsNoTracking()
                .Include(t => t.Venue)
                .Include(t => t.Client)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        /// <summary>
        /// Obtiene un ticket por id para actualización/borrado.
        /// Se devuelve entidad con tracking.
        /// </summary>
        /// <param name="id">Identificador del ticket.</param>
        /// <returns>Ticket si existe; en caso contrario, null.</returns>
        public async Task<Ticket?> GetForUpdateAsync(int id)
        {
            return await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        /// <summary>
        /// Obtiene el detalle de un ticket incluyendo sus relaciones principales.
        /// </summary>
        /// <param name="id">Identificador del ticket.</param>
        /// <returns>Ticket con detalle si existe; en caso contrario, null.</returns>
        public async Task<Ticket?> GetDetailAsync(int id)
        {
            return await _context.Tickets
                .AsNoTracking()
                .Include(t => t.Venue)
                .Include(t => t.Client)
                .AsSplitQuery()
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        /// <summary>
        /// Persiste una actualización de ticket.
        /// Convención habitual: la entidad se obtiene previamente con tracking mediante <see cref="GetForUpdateAsync"/>.
        /// </summary>
        /// <param name="ticket">Entidad ticket.</param>
        /// <returns>True si se guarda correctamente; en caso contrario, false.</returns>
        public async Task<bool> UpdateAsync(Ticket ticket)
        {
            return await SaveAsync();
        }

        /// <summary>
        /// Indica si existe un ticket con el id especificado.
        /// </summary>
        /// <param name="id">Identificador del ticket.</param>
        /// <returns>True si existe; en caso contrario, false.</returns>
        public async Task<bool> ExistsAsync(int id) =>
            await _context.Tickets.AnyAsync(t => t.Id == id);

        /// <summary>
        /// Crea un ticket y persiste cambios.
        /// </summary>
        /// <param name="ticket">Entidad ticket.</param>
        /// <returns>True si se guarda correctamente; en caso contrario, false.</returns>
        public async Task<bool> CreateAsync(Ticket ticket)
        {
            await _context.Tickets.AddAsync(ticket);
            return await SaveAsync();
        }

        /// <summary>
        /// Elimina un ticket por id y persiste cambios.
        /// Para evitar problemas con entidades proyectadas desde caché, la eliminación se realiza
        /// sobre una entidad cargada con tracking. Si la entidad tiene elementos de venta asociados,
        /// se eliminan también para que no queden huérfanos.
        /// </summary>
        /// <param name="id">Identificador del ticket.</param>
        /// <returns>True si se elimina; false si no existe.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            var ticket = await GetForUpdateAsync(id);
            if (ticket == null) return false;

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var items = await _context.SaleItems
                    .Where(i => i.TicketId == id)
                    .ToListAsync();

                if (items.Count > 0)
                    _context.SaleItems.RemoveRange(items);

                _context.Tickets.Remove(ticket);

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


    }
}
