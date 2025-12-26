using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.DTOs.SaleDTOs;
using QuickTix.Core.Models.Entities;
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
    }
    
}
