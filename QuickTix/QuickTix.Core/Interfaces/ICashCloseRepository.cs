using QuickTix.Contracts.DTOs.ReportDTOs;

namespace QuickTix.Core.Interfaces
{
    /// <summary>
    /// Repositorio de solo lectura del cierre de caja (arqueo). No cachea: un arqueo debe ser exacto.
    /// </summary>
    public interface ICashCloseRepository
    {
        /// <summary>
        /// Genera el arqueo del rango inclusivo de días locales (Europe/Madrid) [from, to].
        /// </summary>
        /// <exception cref="ArgumentException">Si from &gt; to o el rango supera 366 días.</exception>
        Task<CashCloseReportDTO> GetReportAsync(DateOnly from, DateOnly to);
    }
}
