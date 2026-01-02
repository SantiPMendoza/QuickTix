using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuickTix.Core.Interfaces;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Core.Models.Entities;
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

        // 🔹 Obtener todos los ítems
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                var dtos = _mapper.Map<IEnumerable<SaleItemDTO>>(entities);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo los ítems de venta");
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // 🔹 Obtener un ítem por Id
        [HttpGet("{id:int}", Name = "[controller]_GetEntity")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var entity = await _repository.GetAsync(id);
                if (entity == null)
                    return NotFound();

                var dto = _mapper.Map<SaleItemDTO>(entity);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo ítem de venta con ID {id}");
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // 🔹 Obtener solo los ítems de tipo Ticket
        [HttpGet("tickets")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTickets()
        {
            try
            {
                var entities = await _repository.GetTicketsAsync();
                var dtos = _mapper.Map<IEnumerable<SaleItemDTO>>(entities);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo los ítems de tipo Ticket");
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // 🔹 Obtener solo los ítems de tipo Subscription
        [HttpGet("subscriptions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSubscriptions()
        {
            try
            {
                var entities = await _repository.GetSubscriptionsAsync();
                var dtos = _mapper.Map<IEnumerable<SaleItemDTO>>(entities);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo los ítems de tipo Subscription");
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // 🔹 Obtener ítems por venta (SaleId)
        [HttpGet("by-sale/{saleId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBySale(int saleId)
        {
            try
            {
                var entities = await _repository.GetBySaleAsync(saleId);
                if (entities == null || !entities.Any())
                    return NotFound($"No se encontraron ítems para la venta con ID {saleId}");

                var dtos = _mapper.Map<IEnumerable<SaleItemDTO>>(entities);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo ítems para la venta {saleId}");
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}
