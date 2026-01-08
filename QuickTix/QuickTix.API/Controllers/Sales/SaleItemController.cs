using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickTix.Contracts.Common;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Core.Interfaces;
using System.Net;

namespace QuickTix.API.Controllers.Sales
{
    /// <summary>
    /// Controlador API para consultar ítems de venta asociados a tickets y suscripciones.
    ///
    /// Este controlador no hereda de BaseController porque:
    /// - No representa un CRUD genérico sobre una entidad independiente.
    /// - Los ítems de venta forman parte del agregado de una venta (Sale) y se gestionan en ese contexto.
    /// - Expone únicamente endpoints de lectura (listados y consultas filtradas).
    ///
    /// Todas las respuestas siguen el contrato <see cref="ApiResponse{T}"/>.
    /// </summary>
    [Authorize(Roles = "admin,manager")]
    [ApiController]
    [Route("api/[controller]")]
    public class SaleItemController : ControllerBase
    {
        private readonly ISaleItemRepository _saleItemRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<SaleItemController> _logger;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="SaleItemController"/>.
        /// </summary>
        /// <param name="saleItemRepository">Repositorio específico de ítems de venta.</param>
        /// <param name="mapper">Servicio de mapeo entre entidades y DTOs.</param>
        /// <param name="logger">Logger del controlador.</param>
        public SaleItemController(
            ISaleItemRepository saleItemRepository,
            IMapper mapper,
            ILogger<SaleItemController> logger)
        {
            _saleItemRepository = saleItemRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el listado completo de ítems de venta.
        /// </summary>
        /// <returns>Listado de ítems de venta.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var traceId = HttpContext.TraceIdentifier;

            var entities = await _saleItemRepository.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<SaleItemDTO>>(entities);

            return Ok(ApiResponse<IEnumerable<SaleItemDTO>>.Ok(dtos, HttpStatusCode.OK, traceId));
        }

        /// <summary>
        /// Obtiene un ítem de venta por su identificador.
        /// </summary>
        /// <param name="id">Identificador del ítem de venta.</param>
        /// <returns>Ítem de venta solicitado.</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int id)
        {
            var traceId = HttpContext.TraceIdentifier;

            var entity = await _saleItemRepository.GetAsync(id);
            if (entity == null)
            {
                return NotFound(ApiResponse<object>.Fail(
                    HttpStatusCode.NotFound,
                    new[] { "Registro no encontrado." },
                    traceId
                ));
            }

            var dto = _mapper.Map<SaleItemDTO>(entity);

            return Ok(ApiResponse<SaleItemDTO>.Ok(dto, HttpStatusCode.OK, traceId));
        }

        /// <summary>
        /// Obtiene los ítems de venta correspondientes a tickets.
        /// </summary>
        /// <returns>Listado de ítems de venta de tipo ticket.</returns>
        [HttpGet("tickets")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTickets()
        {
            var traceId = HttpContext.TraceIdentifier;

            var entities = await _saleItemRepository.GetTicketsAsync();
            var dtos = _mapper.Map<IEnumerable<SaleItemDTO>>(entities);

            return Ok(ApiResponse<IEnumerable<SaleItemDTO>>.Ok(dtos, HttpStatusCode.OK, traceId));
        }

        /// <summary>
        /// Obtiene los ítems de venta correspondientes a suscripciones.
        /// </summary>
        /// <returns>Listado de ítems de venta de tipo suscripción.</returns>
        [HttpGet("subscriptions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSubscriptions()
        {
            var traceId = HttpContext.TraceIdentifier;

            var entities = await _saleItemRepository.GetSubscriptionsAsync();
            var dtos = _mapper.Map<IEnumerable<SaleItemDTO>>(entities);

            return Ok(ApiResponse<IEnumerable<SaleItemDTO>>.Ok(dtos, HttpStatusCode.OK, traceId));
        }

        /// <summary>
        /// Obtiene los ítems de venta asociados a una venta concreta.
        /// </summary>
        /// <param name="saleId">Identificador de la venta.</param>
        /// <returns>Listado de ítems asociados a la venta.</returns>
        [HttpGet("by-sale/{saleId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBySale(int saleId)
        {
            var traceId = HttpContext.TraceIdentifier;

            var entities = await _saleItemRepository.GetBySaleAsync(saleId);

            // Mantengo tu comportamiento: si no hay ítems, 404.
            if (entities == null || !entities.Any())
            {
                return NotFound(ApiResponse<object>.Fail(
                    HttpStatusCode.NotFound,
                    new[] { $"No se encontraron ítems para la venta con ID {saleId}." },
                    traceId
                ));
            }

            var dtos = _mapper.Map<IEnumerable<SaleItemDTO>>(entities);

            return Ok(ApiResponse<IEnumerable<SaleItemDTO>>.Ok(dtos, HttpStatusCode.OK, traceId));
        }
    }
}
