using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickTix.Contracts.Common;
using QuickTix.Contracts.Enums;
using QuickTix.Core.Interfaces;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Core.Models.Entities;
using System.Net;

namespace QuickTix.API.Controllers
{
    /// <summary>
    /// Controlador API para la gestión de abonos (subscriptions).
    /// Proporciona operaciones CRUD y consultas específicas por cliente.
    /// </summary>
    /// <seealso cref="QuickTix.API.Controllers.BaseController&lt;QuickTix.Core.Models.Entities.Subscription, QuickTix.Contracts.Models.DTOs.SubscriptionDTO, QuickTix.Contracts.Models.DTOs.CreateSubscriptionDTO&gt;" />
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : BaseController<Subscription, SubscriptionDTO, CreateSubscriptionDTO>
    {

        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="SubscriptionController"/>.
        /// </summary>
        /// <param name="repository">Repositorio de abonos.</param>
        /// <param name="mapper">Servicio de mapeo entre entidades y DTOs.</param>
        /// <param name="logger">Logger del controlador.</param>
        public SubscriptionController(
            ISubscriptionRepository repository,
            IMapper mapper,
            ILogger<SubscriptionController> logger)
            : base(repository, mapper, logger)
        {
            _subscriptionRepository = repository;
            _mapper = mapper;
        }

        /// <summary>
        /// Obtiene todos los abonos asociados a un cliente concreto.
        /// </summary>
        /// <param name="clientId">Identificador del cliente.</param>
        /// <returns>Listado de abonos del cliente.</returns>
        [HttpGet("by-client/{clientId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByClient(int clientId)
        {
            var traceId = HttpContext.TraceIdentifier;

            var subs = await _subscriptionRepository.GetByClientAsync(clientId);
            var dtos = _mapper.Map<IEnumerable<SubscriptionDTO>>(subs);

            return Ok(ApiResponse<IEnumerable<SubscriptionDTO>>.Ok(dtos, HttpStatusCode.OK, traceId));
        }

        /// <summary>
        /// Crea un nuevo abono aplicando reglas de negocio como fechas y precio.
        /// </summary>
        /// <param name="dto">DTO con los datos de creación del abono.</param>
        /// <returns>Abono creado.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public override async Task<IActionResult> Create([FromBody] CreateSubscriptionDTO dto)
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

            var subscription = _mapper.Map<Subscription>(dto);

            subscription.StartDate = dto.StartDate;
            subscription.EndDate = CalculateEndDate(dto.StartDate, dto.Duration);
            subscription.Price = CalculatePrice(dto.Category, dto.Duration, dto.VenueId);

            var created = await _repository.CreateAsync(subscription);
            if (!created)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponse<object>.Fail(
                        HttpStatusCode.InternalServerError,
                        new[] { "No se pudo crear el abono." },
                        traceId
                    )
                );
            }

            var createdSub = await _subscriptionRepository.GetAsync(subscription.Id);
            var result = _mapper.Map<SubscriptionDTO>(createdSub);

            var response = ApiResponse<SubscriptionDTO>.Ok(result, HttpStatusCode.Created, traceId);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, response);
        }

        /// <summary>
        /// Calcula la fecha de finalización del abono en función de su duración.
        /// </summary>
        /// <param name="startDate">Fecha de inicio del abono.</param>
        /// <param name="duration">Duración contratada.</param>
        /// <returns>Fecha de fin calculada.</returns>
        private static DateTime CalculateEndDate(DateTime startDate, SubscriptionDuration duration)
        {
            return duration switch
            {
                SubscriptionDuration.Quincenal => startDate.AddDays(15),
                SubscriptionDuration.Mensual => startDate.AddMonths(1),
                SubscriptionDuration.Temporada => startDate.AddMonths(3),
                _ => startDate.AddMonths(1)
            };
        }

        /// <summary>
        /// Calcula el precio del abono según categoría y duración.
        /// Lógica provisional pendiente de parametrización por recinto.
        /// </summary>
        /// <param name="category">Categoría del abonado.</param>
        /// <param name="duration">Duración del abono.</param>
        /// <param name="venueId">Identificador del recinto.</param>
        /// <returns>Precio calculado.</returns>
        [Obsolete]
        private static decimal CalculatePrice(SubscriptionCategory category, SubscriptionDuration duration, int venueId)
        {
            // Regla provisional “general”. Cuando definamos tipos por Venue, esto se reemplaza.
            return (category, duration) switch
            {
                (SubscriptionCategory.Niño, SubscriptionDuration.Quincenal) => 15m,
                (SubscriptionCategory.Niño, SubscriptionDuration.Mensual) => 25m,
                (SubscriptionCategory.Niño, SubscriptionDuration.Temporada) => 60m,

                (SubscriptionCategory.Adulto, SubscriptionDuration.Quincenal) => 20m,
                (SubscriptionCategory.Adulto, SubscriptionDuration.Mensual) => 35m,
                (SubscriptionCategory.Adulto, SubscriptionDuration.Temporada) => 80m,

                (SubscriptionCategory.Jubilado, SubscriptionDuration.Quincenal) => 12m,
                (SubscriptionCategory.Jubilado, SubscriptionDuration.Mensual) => 20m,
                (SubscriptionCategory.Jubilado, SubscriptionDuration.Temporada) => 50m,

                (SubscriptionCategory.FamiliaNumerosa, SubscriptionDuration.Quincenal) => 25m,
                (SubscriptionCategory.FamiliaNumerosa, SubscriptionDuration.Mensual) => 45m,
                (SubscriptionCategory.FamiliaNumerosa, SubscriptionDuration.Temporada) => 100m,

                _ => 0m
            };
        }
    }
}
