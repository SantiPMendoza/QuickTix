using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickTix.Contracts.Common;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Core.Interfaces;
using System.Net;

namespace QuickTix.API.Controllers.Sales
{
    //[Authorize(Roles = "admin,manager")]
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class SaleItemController : ControllerBase
    {
        private readonly ISaleItemRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<SaleItemController> _logger;

        public SaleItemController(ISaleItemRepository repository, IMapper mapper, ILogger<SaleItemController> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var traceId = HttpContext.TraceIdentifier;

            var entities = await _repository.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<SaleItemDTO>>(entities);

            return Ok(ApiResponse<IEnumerable<SaleItemDTO>>.Ok(dtos, HttpStatusCode.OK, traceId));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int id)
        {
            var traceId = HttpContext.TraceIdentifier;

            var entity = await _repository.GetAsync(id);
            if (entity == null)
            {
                return NotFound(ApiResponse<object>.Fail(
                    HttpStatusCode.NotFound,
                    new[] { "Registro no encontrado." },
                    traceId
                ));
            }

            var dto = _mapper.Map<SaleItemDTO>(entity);
            if (dto == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponse<object>.Fail(
                        HttpStatusCode.InternalServerError,
                        new[] { "No se pudo generar el DTO del ítem de venta." },
                        traceId
                    )
                );
            }

            return Ok(ApiResponse<SaleItemDTO>.Ok(dto, HttpStatusCode.OK, traceId));
        }

        [HttpGet("tickets")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTickets()
        {
            var traceId = HttpContext.TraceIdentifier;

            var entities = await _repository.GetTicketsAsync();
            var dtos = _mapper.Map<IEnumerable<SaleItemDTO>>(entities);

            return Ok(ApiResponse<IEnumerable<SaleItemDTO>>.Ok(dtos, HttpStatusCode.OK, traceId));
        }

        [HttpGet("subscriptions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSubscriptions()
        {
            var traceId = HttpContext.TraceIdentifier;

            var entities = await _repository.GetSubscriptionsAsync();
            var dtos = _mapper.Map<IEnumerable<SaleItemDTO>>(entities);

            return Ok(ApiResponse<IEnumerable<SaleItemDTO>>.Ok(dtos, HttpStatusCode.OK, traceId));
        }

        [HttpGet("by-sale/{saleId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBySale(int saleId)
        {
            var traceId = HttpContext.TraceIdentifier;

            var entities = await _repository.GetBySaleAsync(saleId);
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
