using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuickTix.Contracts.DTOs.AnalyticsDTOs;
using QuickTix.Core.Interfaces;
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
    /// Criterio de fechas: las ventas se guardan con DateTime.UtcNow
    /// (ver SaleRepository), así que "hoy" se calcula también en UTC.
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

        // Clave de caché del resumen
        private const string CacheKey = "AnalyticsSummaryCacheKey";

        // Tiempo de expiración de la caché (en segundos)
        private const int CacheExpirationTime = 30;

        // Número de ventas recientes devueltas al Panel
        private const int RecentSalesCount = 8;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="AnalyticsRepository"/>.
        /// </summary>
        /// <param name="context">DbContext de la aplicación.</param>
        /// <param name="cache">Caché en memoria.</param>
        public AnalyticsRepository(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        /// <inheritdoc />
        public async Task<AnalyticsSummaryDTO> GetSummaryAsync()
        {
            if (_cache.TryGetValue(CacheKey, out AnalyticsSummaryDTO? cached) && cached != null)
                return cached;

            var nowUtc = DateTime.UtcNow;
            var todayUtc = nowUtc.Date;
            var tomorrowUtc = todayUtc.AddDays(1);
            var weekStartUtc = todayUtc.AddDays(-6);

            // Temporada = año natural en curso (UTC): para una piscina de verano
            // el acumulado del año coincide con la temporada. Fechas reales de
            // apertura/cierre pendientes de definir con Raquel (ver AnalyticsSummaryDTO).
            var seasonStartUtc = new DateTime(todayUtc.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Una única consulta trae las líneas de venta desde el inicio de la
            // temporada (o de la semana, si ésta empieza antes: primeros días de
            // enero) y de ella salen hoy, el desglose por tipo, la temporada y
            // la gráfica de 7 días.
            var itemsFromUtc = weekStartUtc < seasonStartUtc ? weekStartUtc : seasonStartUtc;
            var saleLines = await _context.SaleItems
                .AsNoTracking()
                .Where(i => i.Sale.Date >= itemsFromUtc && i.Sale.Date < tomorrowUtc)
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
                .Where(l => l.Date >= todayUtc)
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
                .GroupBy(l => l.Date.Date)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.UnitPrice * l.Quantity));

            var revenueLast7Days = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var day = weekStartUtc.AddDays(offset);
                    return new DailyRevenueDTO
                    {
                        Date = day,
                        Amount = revenueByDay.TryGetValue(day, out var amount) ? amount : 0m
                    };
                })
                .ToList();

            // --- Distribución de unidades por tipo (histórico completo) ---
            var ticketUnits = await _context.SaleItems
                .AsNoTracking()
                .Where(i => i.TicketId != null)
                .SumAsync(i => (int?)i.Quantity) ?? 0;

            var subscriptionUnits = await _context.SaleItems
                .AsNoTracking()
                .Where(i => i.SubscriptionId != null)
                .SumAsync(i => (int?)i.Quantity) ?? 0;

            // --- Ventas recientes ---
            // El total por venta se calcula en memoria (agregado decimal) sobre
            // las líneas ya proyectadas; son como mucho RecentSalesCount ventas.
            var recentSaleRows = await _context.Sales
                .AsNoTracking()
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
                summary,
                new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(CacheExpirationTime))
            );

            return summary;
        }
    }
}
