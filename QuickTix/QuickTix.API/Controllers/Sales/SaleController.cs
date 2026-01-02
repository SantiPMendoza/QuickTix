using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuickTix.Core.Interfaces;
using QuickTix.Contracts.Models.DTOs.SaleDTO;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Repositories;
using System.Net;

namespace QuickTix.API.Controllers.Sales
{
    //[Authorize(Roles = "admin,manager")]
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class SaleController : BaseController<Sale, SaleDTO, CreateSaleDTO>
    {
        public SaleController(ISaleRepository repository, IMapper mapper, ILogger<SaleController> logger)
            : base(repository, mapper, logger)
        {

        }

        [HttpGet("history/tickets")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTicketHistory()
        {
            try
            {
                var result = await ((ISaleRepository)_repository).GetTicketHistoryAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo historial de tickets");
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet("history/subscriptions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSubscriptionHistory()
        {
            try
            {
                var result = await ((ISaleRepository)_repository).GetSubscriptionHistoryAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo historial de suscripciones");
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost("sell/tickets")]
        public async Task<IActionResult> SellTickets([FromBody] SellTicketDTO request)
        {
            try
            {
                var sale = await ((ISaleRepository)_repository).SellTicketsAsync(request);
                _logger.LogInformation("Venta de tickets registrada. SaleId={SaleId}", sale.Id);

                // Si no necesitas el detalle, puedes devolver solo sale.Id.
                var dto = _mapper.Map<SaleDTO>(sale);
                return Ok(dto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Petición inválida al vender tickets");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando venta de tickets");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("sell/subscription")]
        public async Task<IActionResult> SellSubscription([FromBody] SellSubscriptionDTO request)
        {
            try
            {
                var sale = await ((ISaleRepository)_repository).SellSubscriptionAsync(request);
                _logger.LogInformation("Venta de suscripción registrada. SaleId={SaleId}", sale.Id);

                var dto = _mapper.Map<SaleDTO>(sale);
                return Ok(dto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Petición inválida al vender suscripción");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando venta de suscripción");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
    
}
