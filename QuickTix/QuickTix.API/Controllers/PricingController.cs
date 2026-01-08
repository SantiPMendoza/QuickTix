using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickTix.Contracts.Common;
using QuickTix.Contracts.Models.DTOs.Pricing;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities.Price;
using QuickTix.Core.Services;
using System.Net;

namespace QuickTix.API.Controllers.Pricing
{
    /// <summary>
    /// Controlador API para la gestión de mapas de precios por recinto (Venue).
    /// 
    /// Este controlador no hereda de BaseController porque:
    /// - No representa un CRUD genérico sobre una entidad única.
    /// - Opera sobre un agregado lógico (VenuePriceMap) compuesto por varias entidades.
    /// - Expone endpoints específicos de lectura y upsert, no operaciones CRUD estándar.
    /// 
    /// Aun así, todas las respuestas siguen el contrato ApiResponse{T}.
    /// </summary>
    [Authorize(Roles = "admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class PricingController : ControllerBase
    {
        // Repositorio encargado de recuperar y persistir mapas de precios por recinto
        private readonly IPricingRepository _pricingRepository;

        // Logger del controlador
        private readonly ILogger<PricingController> _logger;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="PricingController"/>.
        /// </summary>
        /// <param name="pricingRepository">Repositorio de precios.</param>
        /// <param name="logger">Logger del controlador.</param>
        public PricingController(IPricingRepository pricingRepository, ILogger<PricingController> logger)
        {
            _pricingRepository = pricingRepository;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el mapa completo de precios de tickets y suscripciones para un recinto.
        /// </summary>
        /// <param name="venueId">Identificador del recinto.</param>
        /// <returns>Mapa de precios del recinto.</returns>
        [HttpGet("venue/{venueId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetVenuePriceMap(int venueId)
        {
            var traceId = HttpContext.TraceIdentifier;

            var map = await _pricingRepository.GetVenuePriceMapAsync(venueId);

            var dto = new VenuePriceMapDTO
            {
                VenueId = map.VenueId,
                TicketPrices = map.TicketPrices.Select(x => new VenueTicketPriceDTO
                {
                    VenueId = x.VenueId,
                    Type = x.Type,
                    Context = x.Context,
                    Price = x.Price
                }).ToList(),
                SubscriptionPrices = map.SubscriptionPrices.Select(x => new VenueSubscriptionPriceDTO
                {
                    VenueId = x.VenueId,
                    Category = x.Category,
                    Duration = x.Duration,
                    Price = x.Price
                }).ToList()
            };

            return Ok(ApiResponse<VenuePriceMapDTO>.Ok(dto, HttpStatusCode.OK, traceId));
        }

        /// <summary>
        /// Inserta o actualiza el mapa de precios completo de un recinto.
        /// Valida coherencia y completitud antes de persistir los datos.
        /// </summary>
        /// <param name="venueId">Identificador del recinto.</param>
        /// <param name="request">Datos del mapa de precios.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpPut("venue/{venueId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpsertVenuePriceMap(int venueId, [FromBody] UpsertVenuePriceMapDTO request)
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

            if (request.VenueId != venueId)
            {
                return BadRequest(ApiResponse<object>.Fail(
                    HttpStatusCode.BadRequest,
                    new[] { "El VenueId del payload no coincide con la ruta." },
                    traceId
                ));
            }

            var map = new VenuePriceMap
            {
                VenueId = venueId,
                TicketPrices = request.TicketPrices.Select(x => new VenueTicketPrice
                {
                    VenueId = venueId,
                    Type = x.Type,
                    Context = x.Context,
                    Price = x.Price
                }).ToList(),
                SubscriptionPrices = request.SubscriptionPrices.Select(x => new VenueSubscriptionPrice
                {
                    VenueId = venueId,
                    Category = x.Category,
                    Duration = x.Duration,
                    Price = x.Price
                }).ToList()
            };

            PriceMapValidator.ValidateNonNegativePrices(map.TicketPrices, map.SubscriptionPrices);

            var mapErrors = PriceMapValidator.ValidateCompleteness(map.TicketPrices, map.SubscriptionPrices);
            if (mapErrors.Count > 0)
            {
                return BadRequest(ApiResponse<object>.Fail(HttpStatusCode.BadRequest, mapErrors, traceId));
            }

            await _pricingRepository.UpsertVenuePriceMapAsync(map);

            _logger.LogInformation("Mapa de precios guardado. VenueId={VenueId}", venueId);

            return Ok(ApiResponse<object>.Ok(
                new { Message = "Mapa de precios guardado correctamente." },
                HttpStatusCode.OK,
                traceId
            ));
        }
    }
}
