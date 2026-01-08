using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Repositories
{
    /// <summary>
    /// Repositorio de acceso a datos para <see cref="Client"/>.
    /// Incluye caché en lecturas de listado para reducir carga de consultas.
    ///
    /// </summary>
    public class ClientRepository : IClientRepository
    {
        // Contexto EF Core de la aplicación.
        private readonly ApplicationDbContext _context;

        // Caché en memoria para acelerar lecturas.
        private readonly IMemoryCache _cache;

        // Clave de caché para la colección de clientes.
        private readonly string _cacheKey = "ClientCacheKey";

        // Duración de expiración de la caché (en segundos).
        private readonly int _cacheExpirationTime = 3600;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="ClientRepository"/>.
        /// </summary>
        /// <param name="context">DbContext de la aplicación.</param>
        /// <param name="cache">Caché en memoria.</param>
        public ClientRepository(ApplicationDbContext context, IMemoryCache cache)
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
        /// Invalida la caché de clientes.
        /// </summary>
        public void ClearCache() => _cache.Remove(_cacheKey);

        /// <summary>
        /// Obtiene el listado de clientes.
        /// Usa caché para evitar consultas repetitivas.
        /// Se proyectan campos mínimos del <see cref="AppUser"/> asociado.
        /// </summary>
        /// <returns>Colección de clientes.</returns>
        public async Task<ICollection<Client>> GetAllAsync()
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Client> cachedClients))
                return cachedClients;

            var clients = await _context.Clients
                .AsNoTracking()
                .Select(c => new Client
                {
                    Id = c.Id,
                    Name = c.Name,
                    AppUserId = c.AppUserId,
                    AppUser = new AppUser
                    {
                        Email = c.AppUser.Email,
                        PhoneNumber = c.AppUser.PhoneNumber,
                        Nif = c.AppUser.Nif
                    }
                })
                .OrderBy(c => c.Id)
                .ToListAsync();

            _cache.Set(_cacheKey, clients, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheExpirationTime)));

            return clients;
        }

        /// <summary>
        /// Obtiene el detalle completo de un cliente para mostrar en UI (panel derecho).
        /// Incluye el usuario asociado y colecciones relacionadas (suscripciones y tickets).
        /// Se ejecuta como split query para reducir el riesgo de explosión cartesiana.
        /// </summary>
        /// <param name="id">Identificador del cliente.</param>
        /// <returns>Cliente con detalle si existe; en caso contrario, null.</returns>
        public async Task<Client?> GetDetailAsync(int id)
        {
            return await _context.Clients
                .AsNoTracking()
                .Include(c => c.AppUser)
                .Include(c => c.Subscriptions)
                .Include(c => c.Tickets)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// Obtiene un cliente por id para actualización/borrado.
        /// La entidad se devuelve con tracking e incluye el <see cref="AppUser"/> asociado.
        /// No incluye colecciones para evitar carga innecesaria en operaciones de escritura.
        /// </summary>
        /// <param name="id">Identificador del cliente.</param>
        /// <returns>Cliente si existe; en caso contrario, null.</returns>
        public async Task<Client?> GetForUpdateAsync(int id)
        {
            return await _context.Clients
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// Obtiene un cliente por id para lectura.
        /// Si existe caché del listado, se reutiliza para evitar consulta a BD.
        /// Si no hay caché, realiza una lectura ligera (sin colecciones).
        /// </summary>
        /// <param name="id">Identificador del cliente.</param>
        /// <returns>Cliente si existe; en caso contrario, null.</returns>
        public async Task<Client?> GetAsync(int id)
        {
            if (_cache.TryGetValue(_cacheKey, out ICollection<Client> cachedClients))
                return cachedClients.FirstOrDefault(c => c.Id == id);

            return await _context.Clients
                .AsNoTracking()
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// Indica si existe un cliente con el id especificado.
        /// </summary>
        /// <param name="id">Identificador del cliente.</param>
        /// <returns>True si existe; en caso contrario, false.</returns>
        public async Task<bool> ExistsAsync(int id) =>
            await _context.Clients.AnyAsync(c => c.Id == id);

        /// <summary>
        /// Crea un cliente y persiste cambios.
        /// </summary>
        /// <param name="client">Entidad cliente.</param>
        /// <returns>True si se guarda correctamente; en caso contrario, false.</returns>
        public async Task<bool> CreateAsync(Client client)
        {
            await _context.Clients.AddAsync(client);
            return await SaveAsync();
        }

        /// <summary>
        /// Persiste una actualización de cliente.
        /// Convención habitual: la entidad se obtiene previamente con tracking mediante <see cref="GetForUpdateAsync"/>.
        /// </summary>
        /// <param name="client">Entidad cliente.</param>
        /// <returns>True si se guarda correctamente; en caso contrario, false.</returns>
        public async Task<bool> UpdateAsync(Client client)
        {
            // Se asume entidad trackeada; si no lo estuviera, habría que adjuntarla/actualizarla explícitamente.
            return await SaveAsync();
        }

        /// <summary>
        /// Elimina un cliente por id y persiste cambios.
        /// Importante: para evitar problemas con entidades proyectadas desde caché,
        /// la eliminación se realiza siempre sobre una entidad cargada con tracking.
        /// </summary>
        /// <param name="id">Identificador del cliente.</param>
        /// <returns>True si se elimina; false si no existe.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            var client = await GetForUpdateAsync(id);
            if (client == null) return false;

            _context.Clients.Remove(client);
            return await SaveAsync();
        }
    }
}
