namespace QuickTix.Contracts.DTOs.AnalyticsDTOs
{
    /// <summary>
    /// Resumen agregado de actividad para el Panel (dashboard) de escritorio.
    /// Solo lectura: lo construye la API a partir de las ventas existentes.
    /// </summary>
    public class AnalyticsSummaryDTO
    {
        /// <summary>Ingresos de hoy (suma de líneas de venta con fecha de hoy, UTC).</summary>
        public decimal RevenueToday { get; set; }

        /// <summary>Unidades de entradas vendidas hoy (UTC).</summary>
        public int TicketsSoldToday { get; set; }

        /// <summary>Abonos vigentes ahora mismo (StartDate &lt;= ahora &lt;= EndDate).</summary>
        public int ActiveSubscriptions { get; set; }

        /// <summary>
        /// Aforo estimado hoy. Con los datos actuales equivale a las entradas
        /// vendidas hoy (son de uso diario): no existen datos de accesos reales,
        /// no se cuentan abonados (sin registro de entrada) y las entradas
        /// Familiar/Grupo cuentan como 1 unidad.
        /// </summary>
        public int EstimatedAttendanceToday { get; set; }

        /// <summary>Ingresos por día de los últimos 7 días (hoy incluido), en orden cronológico.</summary>
        public List<DailyRevenueDTO> RevenueLast7Days { get; set; } = new();

        /// <summary>Distribución histórica de unidades vendidas por tipo (entradas vs abonos).</summary>
        public SalesByTypeDTO SalesByType { get; set; } = new();

        /// <summary>Últimas ventas registradas (máx. 8), de más reciente a más antigua.</summary>
        public List<RecentSaleDTO> RecentSales { get; set; } = new();
    }

    /// <summary>Ingresos de un día concreto.</summary>
    public class DailyRevenueDTO
    {
        /// <summary>Día (fecha UTC, sin componente horario).</summary>
        public DateTime Date { get; set; }

        /// <summary>Importe total del día.</summary>
        public decimal Amount { get; set; }
    }

    /// <summary>Unidades vendidas por tipo de producto.</summary>
    public class SalesByTypeDTO
    {
        /// <summary>Unidades de entradas vendidas (histórico completo).</summary>
        public int TicketUnits { get; set; }

        /// <summary>Unidades de abonos vendidos (histórico completo).</summary>
        public int SubscriptionUnits { get; set; }
    }

    /// <summary>Fila compacta de venta reciente para la tabla del Panel.</summary>
    public class RecentSaleDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;

        /// <summary>Número de unidades (líneas x cantidad) de la venta.</summary>
        public int ItemCount { get; set; }

        public decimal TotalAmount { get; set; }
    }
}
