using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickTix.Contracts.Common;
using QuickTix.Contracts.Models.DTOs.SaleDTO;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Contracts.Models.DTOs.SalesHistory;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using System.Net;

namespace QuickTix.API.Controllers.Sales
{
    //[Authorize(Roles = "admin,manager")]
    // Recomendación: elimina AllowAnonymous si esto debe estar protegido.
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class SaleController : BaseController<Sale, SaleDTO, CreateSaleDTO>
    {
        private readonly ISaleRepository _saleRepository;
        private readonly IMapper _mapper;

        public SaleController(ISaleRepository repository, IMapper mapper, ILogger<SaleController> logger)
            : base(repository, mapper, logger)
        {
            _saleRepository = repository;
            _mapper = mapper;
        }

        [HttpGet("history/tickets")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTicketHistory()
        {
            var traceId = HttpContext.TraceIdentifier;

            var result = await _saleRepository.GetTicketHistoryAsync();
            return Ok(ApiResponse<List<TicketSaleDTO>>.Ok(result.ToList(), HttpStatusCode.OK, traceId));
        }

        [HttpGet("history/subscriptions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSubscriptionHistory()
        {
            var traceId = HttpContext.TraceIdentifier;

            var result = await _saleRepository.GetSubscriptionHistoryAsync(); // debe devolver List<SubscriptionSaleDTO>
            return Ok(ApiResponse<List<SubscriptionSaleDTO>>.Ok(result.ToList(), HttpStatusCode.OK, traceId));
        }

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

            // Si el repo lanza ArgumentException, ApiExceptionFilter la convertirá a 400 automáticamente.
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
