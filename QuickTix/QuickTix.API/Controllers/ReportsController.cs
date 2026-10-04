using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickTix.Contracts.Common;
using QuickTix.Contracts.DTOs.ReportDTOs;
using QuickTix.Core.Interfaces;
using System.Globalization;
using System.Net;

namespace QuickTix.API.Controllers
{
    /// <summary>
    /// Controlador API de informes de administración (solo lectura).
    ///
    /// No hereda de BaseController: no es un CRUD sobre una entidad sino un agregado
    /// calculado a partir de las ventas (mismo criterio que AnalyticsController).
    /// Todas las respuestas siguen el contrato ApiResponse{T}.
    /// </summary>
    [Authorize(Roles = "admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private const string DateFormat = "yyyy-MM-dd";

        // Repositorio de solo lectura del cierre de caja
        private readonly ICashCloseRepository _cashCloseRepository;

        // Logger del controlador
        private readonly ILogger<ReportsController> _logger;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="ReportsController"/>.
        /// </summary>
        /// <param name="cashCloseRepository">Repositorio del cierre de caja.</param>
        /// <param name="logger">Logger del controlador.</param>
        public ReportsController(ICashCloseRepository cashCloseRepository, ILogger<ReportsController> logger)
        {
            _cashCloseRepository = cashCloseRepository;
            _logger = logger;
        }

        /// <summary>
        /// Cierre de caja (arqueo) de un rango inclusivo de días locales (Europe/Madrid).
        /// </summary>
        /// <param name="from">Primer día, formato yyyy-MM-dd.</param>
        /// <param name="to">Último día (incluido), formato yyyy-MM-dd.</param>
        /// <returns>Informe con totales, desglose por recinto/vendedor, concepto y medio de pago, y líneas.</returns>
        [HttpGet("cash-close")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCashClose([FromQuery] string? from, [FromQuery] string? to)
        {
            var traceId = HttpContext.TraceIdentifier;

            if (!TryParseDate(from, out var fromDate) || !TryParseDate(to, out var toDate))
                return BadRequest(Fail(traceId, "Las fechas 'from' y 'to' son obligatorias y deben tener el formato yyyy-MM-dd."));

            try
            {
                var report = await _cashCloseRepository.GetReportAsync(fromDate, toDate);
                return Ok(ApiResponse<CashCloseReportDTO>.Ok(report, HttpStatusCode.OK, traceId));
            }
            catch (ArgumentException ex)
            {
                // El repositorio solo lanza ArgumentException por rango inválido (from > to o > 366 días)
                return BadRequest(Fail(traceId, ex.Message));
            }
        }

        private static bool TryParseDate(string? value, out DateOnly date)
            => DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

        private static ApiResponse<object> Fail(string traceId, string message)
            => ApiResponse<object>.Fail(HttpStatusCode.BadRequest, new[] { message }, traceId);
    }
}
