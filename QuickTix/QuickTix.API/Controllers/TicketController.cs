using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuickTix.Contracts.Common;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using System.Net;

namespace QuickTix.API.Controllers
{
    /// <summary>
    /// Controlador API para la gestión CRUD de tickets.
    /// Reutiliza las operaciones comunes definidas en BaseController.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TicketController : BaseController<Ticket, TicketDTO, CreateTicketDTO>
    {
        // Repositorio específico de ítems de venta para operaciones relacionadas
        private readonly ISaleItemRepository _saleItemRepository;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="TicketController"/>.
        /// </summary>
        /// <param name="repository">Repositorio de tickets.</param>
        /// <param name="mapper">Servicio de mapeo entre entidades y DTOs.</param>
        /// <param name="logger">Logger del controlador.</param>
        public TicketController(ITicketRepository repository, ISaleItemRepository saleItemRepository, IMapper mapper, ILogger<TicketController> logger)
            : base(repository, mapper, logger)
        {
            _saleItemRepository = saleItemRepository;
        }

        /// <summary>
        /// Elimina un registro por su identificador.
        /// </summary>
        /// <param name="id">Identificador del registro.</param>
        /// <returns>
        /// Respuesta de éxito sin payload.
        /// </returns>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public override async Task<IActionResult> Delete(int id)
        {
            var traceId = HttpContext.TraceIdentifier;
            var force = string.Equals(Request.Query["force"], "true", StringComparison.OrdinalIgnoreCase);

            var entity = await _repository.GetAsync(id);
            if (entity == null)
                return NotFound(BuildFail(HttpStatusCode.NotFound, new[] { "Registro no encontrado." }));

            var count = await _saleItemRepository.CountByTicketAsync(id);
            if (count > 0 && !force)
            {
                return Conflict(ApiResponse<object>.Fail(
                    HttpStatusCode.Conflict,
                    new[]
                    {
                $"Este ticket tiene {count} ítems de venta asociados.",
                "Si continúas, se eliminarán también esos ítems de venta.",
                "Repite la operación con ?force=true para confirmar."
                    },
                    traceId
                ));
            }

            var ok = await _repository.DeleteAsync(id);
            if (!ok)
                return NotFound(BuildFail(HttpStatusCode.NotFound, new[] { "Registro no encontrado." }));

            return Ok(ApiResponse<object>.Ok(new { Message = "Ticket eliminado correctamente." }, HttpStatusCode.OK, traceId));
        }
    }
}
