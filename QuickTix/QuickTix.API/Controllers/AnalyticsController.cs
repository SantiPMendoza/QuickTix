using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickTix.Contracts.Common;
using QuickTix.Contracts.DTOs.AnalyticsDTOs;
using QuickTix.Core.Interfaces;
using System.Net;

namespace QuickTix.API.Controllers
{
    /// <summary>
    /// Controlador API del resumen de analítica para el Panel de escritorio.
    ///
    /// Este controlador no hereda de BaseController porque:
    /// - No representa un CRUD sobre una entidad: es un agregado de solo lectura
    ///   calculado a partir de ventas, entradas y abonos existentes.
    /// - Expone un único endpoint GET, sin operaciones de escritura.
    ///
    /// Aun así, todas las respuestas siguen el contrato ApiResponse{T}.
    /// </summary>
    [Authorize(Roles = "admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        // Repositorio de solo lectura con las consultas agregadas del Panel
        private readonly IAnalyticsRepository _analyticsRepository;

        // Logger del controlador
        private readonly ILogger<AnalyticsController> _logger;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="AnalyticsController"/>.
        /// </summary>
        /// <param name="analyticsRepository">Repositorio de analítica.</param>
        /// <param name="logger">Logger del controlador.</param>
        public AnalyticsController(IAnalyticsRepository analyticsRepository, ILogger<AnalyticsController> logger)
        {
            _analyticsRepository = analyticsRepository;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el resumen agregado para el Panel: KPIs del día, ingresos de los
        /// últimos 7 días, distribución de ventas por tipo y ventas recientes.
        /// </summary>
        /// <returns>Resumen de analítica.</returns>
        [HttpGet("summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary()
        {
            var traceId = HttpContext.TraceIdentifier;

            var summary = await _analyticsRepository.GetSummaryAsync();

            return Ok(ApiResponse<AnalyticsSummaryDTO>.Ok(summary, HttpStatusCode.OK, traceId));
        }
    }
}
