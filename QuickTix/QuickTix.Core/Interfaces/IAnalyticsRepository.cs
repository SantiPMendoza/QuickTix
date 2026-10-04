using QuickTix.Contracts.DTOs.AnalyticsDTOs;

namespace QuickTix.Core.Interfaces
{
    /// <summary>
    /// Repositorio de solo lectura para consultas agregadas de analítica (Panel de escritorio).
    /// No implementa <see cref="IRepository{TEntity}"/> ni expone SaveAsync: no persiste nada,
    /// por lo que no participa en la decisión pendiente sobre la frontera de transacción (ADR-002).
    /// </summary>
    public interface IAnalyticsRepository
    {
        /// <summary>
        /// Obtiene el resumen agregado de actividad: KPIs del día, ingresos de los
        /// últimos 7 días, distribución de unidades por tipo y ventas recientes.
        /// </summary>
        /// <returns>Resumen para el Panel.</returns>
        Task<AnalyticsSummaryDTO> GetSummaryAsync();
    }
}
