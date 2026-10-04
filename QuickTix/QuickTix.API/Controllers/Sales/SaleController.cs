using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickTix.Contracts.Common;
using QuickTix.Contracts.DTOs.SaleDTOs;
using QuickTix.Contracts.DTOs.SaleDTOs.Subscription;
using QuickTix.Contracts.DTOs.SaleDTOs.Ticket;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using System.Net;
using System.Security.Claims;

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

        // Repositorio de analítica: al anular hay que invalidar su caché (TTL 30 s) para que el Panel baje al instante
        private readonly IAnalyticsRepository _analyticsRepository;

        // Reloj inyectable (tests deterministas); en producción TimeProvider.System
        private readonly TimeProvider _clock;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="SaleController"/>.
        /// </summary>
        /// <param name="repository">Repositorio de ventas.</param>
        /// <param name="mapper">Servicio de mapeo entre entidades y DTOs.</param>
        /// <param name="logger">Logger del controlador.</param>
        /// <param name="analyticsRepository">Repositorio de analítica (invalidación de caché al anular).</param>
        /// <param name="clock">Reloj (UTC) para fechar las anulaciones.</param>
        public SaleController(
            ISaleRepository repository,
            IMapper mapper,
            ILogger<SaleController> logger,
            IAnalyticsRepository analyticsRepository,
            TimeProvider clock)
            : base(repository, mapper, logger)
        {
            _saleRepository = repository;
            _analyticsRepository = analyticsRepository;
            _clock = clock;
        }

        /// <summary>
        /// Obtiene el historial de ventas de tickets.
        /// </summary>
        /// <returns>Listado de ventas de tickets.</returns>
        [HttpGet("history/tickets")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTicketHistory()
        {
            var result = await _saleRepository.GetTicketHistoryAsync();
            return Ok(BuildOk(result.ToList(), HttpStatusCode.OK));
        }

        /// <summary>
        /// Obtiene el detalle completo de una venta de ticket concreta.
        /// </summary>
        /// <param name="saleId">Identificador de la venta.</param>
        /// <returns>Detalle de la venta de ticket.</returns>
        [HttpGet("history/tickets/{saleId:int}/detail")]
        public async Task<IActionResult> GetTicketHistoryDetail(int saleId)
        {
            var data = await _saleRepository.GetTicketHistoryDetailAsync(saleId);
            return Ok(BuildOk(data, HttpStatusCode.OK));
        }

        /// <summary>
        /// Obtiene el historial de ventas de suscripciones.
        /// </summary>
        /// <returns>Listado de ventas de suscripciones.</returns>
        [HttpGet("history/subscriptions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSubscriptionHistory()
        {
            var result = await _saleRepository.GetSubscriptionHistoryAsync();
            return Ok(BuildOk(result.ToList(), HttpStatusCode.OK));
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
            if (!ModelState.IsValid)
            {
                return BadRequest(BuildFail(
                    HttpStatusCode.BadRequest,
                    ExtractModelStateErrors(ModelState)
                ));
            }

            var sale = await _saleRepository.SellTicketsAsync(request);

            _logger.LogInformation("Venta de tickets registrada. SaleId={SaleId}", sale.Id);

            var dto = _mapper.Map<SaleDTO>(sale);
            if (dto == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    BuildFail(
                        HttpStatusCode.InternalServerError,
                        new[] { "La venta se registró pero no se pudo generar el DTO de respuesta." }
                    )
                );
            }

            return Ok(BuildOk(dto, HttpStatusCode.OK));
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
            if (!ModelState.IsValid)
            {
                return BadRequest(BuildFail(
                    HttpStatusCode.BadRequest,
                    ExtractModelStateErrors(ModelState)
                ));
            }

            var sale = await _saleRepository.SellTicketsBatchAsync(request);

            _logger.LogInformation("Venta batch de tickets registrada. SaleId={SaleId}", sale.Id);

            var dto = _mapper.Map<SaleDTO>(sale);
            if (dto == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    BuildFail(
                        HttpStatusCode.InternalServerError,
                        new[] { "La venta se registró pero no se pudo generar el DTO de respuesta." }
                    )
                );
            }

            return Ok(BuildOk(dto, HttpStatusCode.OK));
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
            if (!ModelState.IsValid)
            {
                return BadRequest(BuildFail(
                    HttpStatusCode.BadRequest,
                    ExtractModelStateErrors(ModelState)
                ));
            }

            // Solo un admin puede registrar ventas sin manager (ManagerId null = "Administración")
            if (!request.ManagerId.HasValue && !User.IsInRole("admin"))
            {
                return BadRequest(BuildFail(
                    HttpStatusCode.BadRequest,
                    new[] { "Un manager debe indicar su managerId para registrar la venta." }
                ));
            }

            var sale = await _saleRepository.SellSubscriptionAsync(request);

            _logger.LogInformation("Venta de suscripción registrada. SaleId={SaleId}", sale.Id);

            var dto = _mapper.Map<SaleDTO>(sale);
            if (dto == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    BuildFail(
                        HttpStatusCode.InternalServerError,
                        new[] { "La venta se registró pero no se pudo generar el DTO de respuesta." }
                    )
                );
            }

            return Ok(BuildOk(dto, HttpStatusCode.OK));
        }

        /// <summary>
        /// Las ventas no se crean por el POST genérico: entran solo por los endpoints sell/*,
        /// que calculan precios en servidor y fijan el vendedor.
        /// </summary>
        // El POST genérico mapeaba CreateSaleDTO a una Sale sin líneas ni precios (y dejaba elegir
        // ManagerId/VenueId libremente): una venta fantasma o con vendedor falseado en el arqueo (E15).
        // Mismo criterio de roles que el Delete: solo admin llega aquí, y siempre recibe 400.
        [Authorize(Roles = "admin")]
        public override Task<IActionResult> Create([FromBody] CreateSaleDTO createDto)
        {
            return Task.FromResult<IActionResult>(BadRequest(BuildFail(
                HttpStatusCode.BadRequest,
                new[] { "Las ventas se registran desde los endpoints de venta." }
            )));
        }

        /// <summary>
        /// Las ventas no se modifican: una venta incorrecta se anula.
        /// </summary>
        // El PUT genérico no persistía nada útil (los campos de anulación y PaymentMethod no existen en
        // SaleDTO) y, sin guard, permitía a un manager reetiquetar ManagerId a null ("Administración").
        // Se rechaza siempre: cualquier corrección contable pasa por anular y volver a vender (E16).
        // Mismo criterio de roles que el Delete: el manager recibe 403 antes de llegar aquí.
        [Authorize(Roles = "admin")]
        public override Task<IActionResult> Update(int id, [FromBody] SaleDTO dto)
        {
            return Task.FromResult<IActionResult>(BadRequest(BuildFail(
                HttpStatusCode.BadRequest,
                new[] { "Las ventas no se modifican: se anulan." }
            )));
        }

        /// <summary>
        /// Anula (lógicamente) una venta: queda en el historial pero fuera del arqueo y del Panel.
        /// Solo admin. No toca los Ticket/Subscription asociados (política pendiente de definir).
        /// </summary>
        /// <param name="id">Identificador de la venta.</param>
        /// <param name="request">Motivo de la anulación (obligatorio, máx. 200 caracteres).</param>
        [HttpPost("{id:int}/void")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Void(int id, [FromBody] VoidSaleDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(BuildFail(
                    HttpStatusCode.BadRequest,
                    ExtractModelStateErrors(ModelState)
                ));
            }

            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub") ??
                User.FindFirstValue("nameid");

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("Token inválido o sin identificador de usuario.");

            var result = await _saleRepository.VoidAsync(id, userId, request.Reason, _clock.GetUtcNow().UtcDateTime);

            switch (result)
            {
                case VoidSaleResult.Voided:
                    // E9: el Panel cachea el resumen 30 s; sin esto la venta anulada seguiría sumando.
                    _analyticsRepository.InvalidateSummaryCache();
                    _logger.LogInformation("Venta anulada. SaleId={SaleId} UserId={UserId}", id, userId);
                    return Ok(BuildOk<object?>(null, HttpStatusCode.OK));

                case VoidSaleResult.NotFound:
                    return NotFound(BuildFail(HttpStatusCode.NotFound, new[] { "Venta no encontrada." }));

                case VoidSaleResult.AlreadyVoided:
                    return Conflict(BuildFail(HttpStatusCode.Conflict, new[] { "La venta ya está anulada." }));

                default:
                    return BadRequest(BuildFail(
                        HttpStatusCode.BadRequest,
                        new[] { $"El motivo de la anulación es obligatorio y no puede superar los {VoidSaleDTO.ReasonMaxLength} caracteres." }
                    ));
            }
        }

        /// <summary>
        /// Las ventas nunca se borran físicamente: borrarlas falsearía el arqueo sin dejar rastro.
        /// </summary>
        // El [Authorize(Roles = "admin")] hace que un manager reciba 403 antes de llegar aquí;
        // un admin recibe 400 indicando la vía correcta. Se elige 400 (y no 405) porque la ruta
        // DELETE existe y el cliente necesita el mensaje en el envelope ApiResponse.
        [Authorize(Roles = "admin")]
        public override Task<IActionResult> Delete(int id)
        {
            return Task.FromResult<IActionResult>(BadRequest(BuildFail(
                HttpStatusCode.BadRequest,
                new[] { "Las ventas no se borran: se anulan." }
            )));
        }
    }
}
