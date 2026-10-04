using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Contracts.DTOs.AnalyticsDTOs;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Time;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Repositories
{
    /// <summary>
    /// Repositorio de solo lectura con las consultas agregadas del Panel (dashboard).
    ///
    /// No expone SaveAsync ni frontera de transacción: todas las consultas son
    /// lecturas AsNoTracking sobre el modelo de ventas existente, por lo que
    /// queda fuera de la decisión aparcada sobre Unit of Work (ADR-002).
    ///
    /// Criterio de fechas: las ventas se guardan en UTC (ver SaleRepository), pero el
    /// "día" del Panel es el día local de Madrid (LocalBusinessDay), el mismo que usa el
    /// arqueo. Las consultas se acotan con el intervalo UTC [inicio, fin) de esos días
    /// locales y las agrupaciones por día se hacen sobre la fecha local.
    ///
    /// Las ventas anuladas (VoidedAt != null) quedan fuera de TODOS los agregados.
    /// Los abonos no se tocan al anular una venta (política pendiente), así que los KPI
    /// de abonos vigentes/por caducar se derivan de la tabla de abonos.
    ///
    /// Los importes (decimal) se suman EN MEMORIA sobre proyecciones compactas:
    /// el provider de SQLite (usado en los tests de integración) no traduce
    /// agregados sobre decimal, y convertir a double rompería la exactitud del
    /// dinero. Con el volumen de una piscina municipal y la caché de 30 s,
    /// traer las líneas de la temporada una vez es más que suficiente.
    /// </summary>
    public class AnalyticsRepository : IAnalyticsRepository
    {
        // Contexto EF Core de la aplicación
        private readonly ApplicationDbContext _context;

        // Caché en memoria, mismo patrón que los repos de lectura frecuente.
        // TTL corto: el Panel tolera datos con hasta 30 s de retraso.
        private readonly IMemoryCache _cache;

        // Reloj inyectable (tests deterministas); en producción TimeProvider.System.
        private readonly TimeProvider _clock;

        // Clave de caché del resumen
        private const string CacheKey = "AnalyticsSummaryCacheKey";

        // Clave de la "generación" de la caché: contador que sube en cada invalidación
        private const string GenerationKey = "AnalyticsSummaryGenerationKey";

        // Tiempo de expiración de la caché (en segundos)
        private const int CacheExpirationTime = 30;

        // El repositorio es scoped pero la IMemoryCache es singleton: el candado serializa el
        // contador compartido entre peticiones concurrentes.
        private static readonly object GenerationLock = new();

        // Resumen cacheado junto a la generación con la que se calculó
        private sealed record CachedSummary(long Generation, AnalyticsSummaryDTO Summary);

        // Número de ventas recientes devueltas al Panel
        private const int RecentSalesCount = 8;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="AnalyticsRepository"/>.
        /// </summary>
        /// <param name="context">DbContext de la aplicación.</param>
        /// <param name="cache">Caché en memoria.</param>
        /// <param name="clock">Reloj (UTC) para calcular "hoy".</param>
        public AnalyticsRepository(ApplicationDbContext context, IMemoryCache cache, TimeProvider clock)
        {
            _context = context;
            _cache = cache;
            _clock = clock;
        }

        /// <inheritdoc />
        public void InvalidateSummaryCache()
        {
            // Subir la generación (y no solo borrar la entrada) cierra la carrera: un resumen
            // calculado ANTES de la anulación que termina DESPUÉS de esta llamada se guardaría
            // con la generación vieja y nunca se serviría.
            lock (GenerationLock)
            {
                _cache.Set(GenerationKey, ReadGeneration() + 1);
            }

            _cache.Remove(CacheKey);
        }

        private long ReadGeneration() => _cache.TryGetValue(GenerationKey, out long generation) ? generation : 0L;

        /// <inheritdoc />
        public async Task<AnalyticsSummaryDTO> GetSummaryAsync()
        {
            // La generación se lee ANTES de calcular: si una invalidación ocurre mientras se calcula,
            // el resultado queda etiquetado con una generación ya vencida.
            long generation;
            lock (GenerationLock)
            {
                generation = ReadGeneration();
            }

            if (_cache.TryGetValue(CacheKey, out CachedSummary? cached)
                && cached != null
                && cached.Generation == generation)
                return cached.Summary;

            var nowUtc = _clock.GetUtcNow().UtcDateTime;

            // "Hoy", la semana y la temporada se definen en días LOCALES de Madrid y se
            // convierten a límites UTC [inicio, fin) para consultar (correcto en cambios de hora).
            var todayLocal = LocalBusinessDay.TodayLocal(nowUtc);
            var weekStartLocal = todayLocal.AddDays(-6);

            // Temporada = año natural local en curso: para una piscina de verano
            // el acumulado del año coincide con la temporada. Fechas reales de
            // apertura/cierre pendientes de definir con Raquel (ver AnalyticsSummaryDTO).
            var seasonStartLocal = new DateOnly(todayLocal.Year, 1, 1);

            var (todayStartUtc, tomorrowUtc) = LocalBusinessDay.ToUtcRange(todayLocal);
            var weekStartUtc = LocalBusinessDay.ToUtcRange(weekStartLocal).StartUtc;
            var seasonStartUtc = LocalBusinessDay.ToUtcRange(seasonStartLocal).StartUtc;

            // Una única consulta trae las líneas de venta desde el inicio de la
            // temporada (o de la semana, si ésta empieza antes: primeros días de
            // enero) y de ella salen hoy, el desglose por tipo, la temporada y
            // la gráfica de 7 días.
            var itemsFromUtc = weekStartUtc < seasonStartUtc ? weekStartUtc : seasonStartUtc;
            var saleLines = await _context.SaleItems
                .AsNoTracking()
                .Where(i => i.Sale.VoidedAt == null && i.Sale.Date >= itemsFromUtc && i.Sale.Date < tomorrowUtc)
                .Select(i => new
                {
                    i.Sale.Date,
                    IsTicket = i.TicketId != null,
                    i.UnitPrice,
                    i.Quantity
                })
                .ToListAsync();

            // --- KPI: ingresos de hoy, desglosados por tipo de línea ---
            // El total del día es la suma de ambos (toda línea es entrada o abono).
            var todayLines = saleLines
                .Where(l => l.Date >= todayStartUtc)
                .ToList();

            var ticketRevenueToday = todayLines
                .Where(l => l.IsTicket)
                .Sum(l => l.UnitPrice * l.Quantity);
            var subscriptionRevenueToday = todayLines
                .Where(l => !l.IsTicket)
                .Sum(l => l.UnitPrice * l.Quantity);
            var revenueToday = ticketRevenueToday + subscriptionRevenueToday;

            // --- KPI: ingresos acumulados de la temporada ---
            var seasonRevenue = saleLines
                .Where(l => l.Date >= seasonStartUtc)
                .Sum(l => l.UnitPrice * l.Quantity);

            // --- KPI: unidades de entradas vendidas hoy ---
            var ticketsSoldToday = todayLines
                .Where(l => l.IsTicket)
                .Sum(l => l.Quantity);

            // --- KPI: abonos vigentes ahora mismo ---
            var activeSubscriptions = await _context.Subscriptions
                .AsNoTracking()
                .CountAsync(s => s.StartDate <= nowUtc && s.EndDate >= nowUtc);

            // --- KPI: abonos vigentes que caducan en los próximos 7 días ---
            // Mismo criterio de vigencia que el KPI anterior (subconjunto suyo):
            // los ya caducados no cuentan, solo los que siguen activos y expiran pronto.
            var expiringWindowEndUtc = nowUtc.AddDays(7);
            var expiringSubscriptionsCount = await _context.Subscriptions
                .AsNoTracking()
                .CountAsync(s => s.StartDate <= nowUtc
                                 && s.EndDate >= nowUtc
                                 && s.EndDate < expiringWindowEndUtc);

            // --- KPI: aforo estimado hoy ---
            // Fórmula elegida: entradas vendidas hoy, porque cada entrada es de
            // uso diario (una entrada vendida hoy = una persona esperada hoy).
            // Limitaciones documentadas (no hay datos para hacerlo mejor):
            // - No incluye abonados: no existe registro de accesos (flujo aparcado).
            // - Familiar/Grupo cuentan como 1 unidad (no se conoce el nº de personas).
            // No se aplican factores estimados: el dato es honesto aunque coincida
            // con "entradas vendidas hoy"; divergirán cuando exista control de accesos.
            var estimatedAttendanceToday = ticketsSoldToday;

            // --- Ingresos por día, últimos 7 días (hoy incluido) ---
            // Se completan los días sin ventas con importe 0.
            var revenueByDay = saleLines
                .Where(l => l.Date >= weekStartUtc)
                .GroupBy(l => LocalBusinessDay.ToLocalDate(l.Date))
                .ToDictionary(g => g.Key, g => g.Sum(l => l.UnitPrice * l.Quantity));

            var revenueLast7Days = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var day = weekStartLocal.AddDays(offset);
                    return new DailyRevenueDTO
                    {
                        Date = day.ToDateTime(TimeOnly.MinValue),
                        Amount = revenueByDay.TryGetValue(day, out var amount) ? amount : 0m
                    };
                })
                .ToList();

            // --- Distribución de unidades por tipo (histórico completo) ---
            var ticketUnits = await _context.SaleItems
                .AsNoTracking()
                .Where(i => i.Sale.VoidedAt == null && i.TicketId != null)
                .SumAsync(i => (int?)i.Quantity) ?? 0;

            var subscriptionUnits = await _context.SaleItems
                .AsNoTracking()
                .Where(i => i.Sale.VoidedAt == null && i.SubscriptionId != null)
                .SumAsync(i => (int?)i.Quantity) ?? 0;

            // --- Ventas recientes ---
            // El total por venta se calcula en memoria (agregado decimal) sobre
            // las líneas ya proyectadas; son como mucho RecentSalesCount ventas.
            var recentSaleRows = await _context.Sales
                .AsNoTracking()
                .Where(s => s.VoidedAt == null)
                .OrderByDescending(s => s.Date)
                .ThenByDescending(s => s.Id)
                .Take(RecentSalesCount)
                .Select(s => new
                {
                    s.Id,
                    s.Date,
                    VenueName = s.Venue.Name,
                    // Ventas de administración no tienen manager asociado
                    ManagerName = s.Manager != null ? s.Manager.Name : "Administración",
                    Lines = s.Items.Select(i => new { i.Quantity, i.UnitPrice }).ToList()
                })
                .ToListAsync();

            var recentSales = recentSaleRows
                .Select(s => new RecentSaleDTO
                {
                    Id = s.Id,
                    Date = s.Date,
                    VenueName = s.VenueName,
                    ManagerName = s.ManagerName,
                    ItemCount = s.Lines.Sum(l => l.Quantity),
                    TotalAmount = s.Lines.Sum(l => l.UnitPrice * l.Quantity)
                })
                .ToList();

            var summary = new AnalyticsSummaryDTO
            {
                RevenueToday = revenueToday,
                TicketRevenueToday = ticketRevenueToday,
                SubscriptionRevenueToday = subscriptionRevenueToday,
                SeasonRevenue = seasonRevenue,
                TicketsSoldToday = ticketsSoldToday,
                ActiveSubscriptions = activeSubscriptions,
                ExpiringSubscriptionsCount = expiringSubscriptionsCount,
                EstimatedAttendanceToday = estimatedAttendanceToday,
                RevenueLast7Days = revenueLast7Days,
                SalesByType = new SalesByTypeDTO
                {
                    TicketUnits = ticketUnits,
                    SubscriptionUnits = subscriptionUnits
                },
                RecentSales = recentSales
            };

            _cache.Set(
                CacheKey,
                new CachedSummary(generation, summary),
                new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(CacheExpirationTime))
            );

            return summary;
        }
    }
}
