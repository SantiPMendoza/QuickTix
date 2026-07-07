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

            // --- KPI: ingresos de hoy (todas las líneas de venta del día) ---
            var revenueToday = await _context.SaleItems
                .AsNoTracking()
                .Where(i => i.Sale.Date >= todayUtc && i.Sale.Date < tomorrowUtc)
                .SumAsync(i => (decimal?)(i.UnitPrice * i.Quantity)) ?? 0m;

            // --- KPI: unidades de entradas vendidas hoy ---
            var ticketsSoldToday = await _context.SaleItems
                .AsNoTracking()
                .Where(i => i.TicketId != null
                            && i.Sale.Date >= todayUtc && i.Sale.Date < tomorrowUtc)
                .SumAsync(i => (int?)i.Quantity) ?? 0;

            // --- KPI: abonos vigentes ahora mismo ---
            var activeSubscriptions = await _context.Subscriptions
                .AsNoTracking()
                .CountAsync(s => s.StartDate <= nowUtc && s.EndDate >= nowUtc);

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
            var revenueRows = await _context.SaleItems
                .AsNoTracking()
                .Where(i => i.Sale.Date >= weekStartUtc && i.Sale.Date < tomorrowUtc)
                .GroupBy(i => i.Sale.Date.Date)
                .Select(g => new { Day = g.Key, Amount = g.Sum(x => x.UnitPrice * x.Quantity) })
                .ToListAsync();

            // Se completan en memoria los días sin ventas con importe 0
            var revenueLast7Days = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var day = weekStartUtc.AddDays(offset);
                    return new DailyRevenueDTO
                    {
                        Date = day,
                        Amount = revenueRows.FirstOrDefault(r => r.Day == day)?.Amount ?? 0m
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
            var recentSales = await _context.Sales
                .AsNoTracking()
                .OrderByDescending(s => s.Date)
                .ThenByDescending(s => s.Id)
                .Take(RecentSalesCount)
                .Select(s => new RecentSaleDTO
                {
                    Id = s.Id,
                    Date = s.Date,
                    VenueName = s.Venue.Name,
                    // Ventas de administración no tienen manager asociado
                    ManagerName = s.Manager != null ? s.Manager.Name : "Administración",
                    ItemCount = s.Items.Sum(i => i.Quantity),
                    TotalAmount = s.Items.Sum(i => i.UnitPrice * i.Quantity)
                })
                .ToListAsync();

            var summary = new AnalyticsSummaryDTO
            {
                RevenueToday = revenueToday,
                TicketsSoldToday = ticketsSoldToday,
                ActiveSubscriptions = activeSubscriptions,
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
