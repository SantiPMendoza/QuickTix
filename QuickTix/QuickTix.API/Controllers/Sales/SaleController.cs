using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickTix.Contracts.Common;
using QuickTix.Contracts.DTOs.SaleDTOs.Subscription;
using QuickTix.Contracts.DTOs.SaleDTOs.Ticket;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using System.Net;

namespace QuickTix.API.Controllers.Sales
{
    /// <summary>
    /// Controlador API para la gestión y registro de ventas de tickets y suscripciones.
    /// Incluye operaciones de venta y consulta de históricos.
    /// </summary>
    [Authorize(Roles = "admin,manager")]
    [Route("api/[controller]")]
    [ApiController]
    public class SaleController : BaseController<Sale, SaleDTO, CreateSaleDTO>
    {
        // Repositorio específico de ventas con lógica de consulta y registro
        private readonly ISaleRepository _saleRepository;

        // Mapper para transformar entidades de dominio en DTOs
        private readonly IMapper _mapper;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="SaleController"/>.
        /// </summary>
        /// <param name="repository">Repositorio de ventas.</param>
        /// <param name="mapper">Servicio de mapeo de entidades.</param>
        /// <param name="logger">Logger del controlador.</param>
        public SaleController(ISaleRepository repository, IMapper mapper, ILogger<SaleController> logger)
            : base(repository, mapper, logger)
        {
            _saleRepository = repository;
            _mapper = mapper;
        }

        /// <summary>
        /// Obtiene el historial de ventas de tickets.
        /// </summary>
        /// <returns>Listado de ventas de tickets.</returns>
        [HttpGet("history/tickets")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTicketHistory()
        {
            var traceId = HttpContext.TraceIdentifier;

            var result = await _saleRepository.GetTicketHistoryAsync();
            return Ok(ApiResponse<List<TicketSaleDTO>>.Ok(result.ToList(), HttpStatusCode.OK, traceId));
        }

        /// <summary>
        /// Obtiene el detalle completo de una venta de ticket concreta.
        /// </summary>
        /// <param name="saleId">Identificador de la venta.</param>
        /// <returns>Detalle de la venta de ticket.</returns>
        [HttpGet("history/tickets/{saleId:int}/detail")]
        public async Task<IActionResult> GetTicketHistoryDetail(int saleId)
        {
            var traceId = HttpContext.TraceIdentifier;

            var data = await _saleRepository.GetTicketHistoryDetailAsync(saleId);
            return Ok(ApiResponse<TicketSaleDetailDTO>.Ok(data, HttpStatusCode.OK, traceId));
        }

        /// <summary>
        /// Obtiene el historial de ventas de suscripciones.
        /// </summary>
        /// <returns>Listado de ventas de suscripciones.</returns>
        [HttpGet("history/subscriptions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSubscriptionHistory()
        {
            var traceId = HttpContext.TraceIdentifier;

            var result = await _saleRepository.GetSubscriptionHistoryAsync();
            return Ok(ApiResponse<List<SubscriptionSaleDTO>>.Ok(result.ToList(), HttpStatusCode.OK, traceId));
        }

        /// <summary>
        /// Registra la venta de uno o varios tickets en una única operación.
        /// </summary>
        /// <param name="request">Datos de la venta de tickets.</param>
        /// <returns>Venta registrada.</returns>
        [HttpPost("sell/tickets")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SellTickets([FromBody] SellTicketDTO request)
        {
            var traceId = HttpContext.TraceIdentifier;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Error de validación." : e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.Fail(HttpStatusCode.BadRequest, errors, traceId));
            }

            var sale = await _saleRepository.SellTicketsAsync(request);

            _logger.LogInformation("Venta de tickets registrada. SaleId={SaleId}", sale.Id);

            var dto = _mapper.Map<SaleDTO>(sale);
            if (dto == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponse<object>.Fail(
                        HttpStatusCode.InternalServerError,
                        new[] { "La venta se registró pero no se pudo generar el DTO de respuesta." },
                        traceId
                    )
                );
            }

            return Ok(ApiResponse<SaleDTO>.Ok(dto, HttpStatusCode.OK, traceId));
        }

        /// <summary>
        /// Registra la venta de tickets en modo batch.
        /// </summary>
        /// <param name="request">Datos de la venta batch de tickets.</param>
        /// <returns>Venta registrada.</returns>
        [HttpPost("sell/tickets/batch")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SellTicketsBatch([FromBody] SellTicketsBatchDTO request)
        {
            var traceId = HttpContext.TraceIdentifier;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Error de validación." : e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.Fail(HttpStatusCode.BadRequest, errors, traceId));
            }

            var sale = await _saleRepository.SellTicketsBatchAsync(request);

            _logger.LogInformation("Venta batch de tickets registrada. SaleId={SaleId}", sale.Id);

            var dto = _mapper.Map<SaleDTO>(sale);
            if (dto == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponse<object>.Fail(
                        HttpStatusCode.InternalServerError,
                        new[] { "La venta se registró pero no se pudo generar el DTO de respuesta." },
                        traceId
                    )
                );
            }

            return Ok(ApiResponse<SaleDTO>.Ok(dto, HttpStatusCode.OK, traceId));
        }

        /// <summary>
        /// Registra la venta de una suscripción.
        /// </summary>
        /// <param name="request">Datos de la venta de la suscripción.</param>
        /// <returns>Venta registrada.</returns>
        [HttpPost("sell/subscription")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SellSubscription([FromBody] SellSubscriptionDTO request)
        {
            var traceId = HttpContext.TraceIdentifier;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Error de validación." : e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.Fail(HttpStatusCode.BadRequest, errors, traceId));
            }

            var sale = await _saleRepository.SellSubscriptionAsync(request);

            _logger.LogInformation("Venta de suscripción registrada. SaleId={SaleId}", sale.Id);

            var dto = _mapper.Map<SaleDTO>(sale);
            if (dto == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponse<object>.Fail(
                        HttpStatusCode.InternalServerError,
                        new[] { "La venta se registró pero no se pudo generar el DTO de respuesta." },
                        traceId
                    )
                );
            }

            return Ok(ApiResponse<SaleDTO>.Ok(dto, HttpStatusCode.OK, traceId));
        }
    }
}
