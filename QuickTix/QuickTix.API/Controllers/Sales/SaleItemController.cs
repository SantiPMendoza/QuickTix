using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickTix.Contracts.Common;
using QuickTix.Contracts.Models.DTOs.SaleDTOs;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using System.Net;

namespace QuickTix.API.Controllers.Sales
{
    /// <summary>
    /// Controlador API para consultar ítems de venta asociados a tickets y suscripciones.
    /// Expone endpoints de lectura para listados y consultas filtradas por venta.
    /// </summary>
    [Authorize(Roles = "admin,manager")]
    [ApiController]
    [Route("api/[controller]")]
    public class SaleItemController : BaseController<SaleItem, SaleItemDTO, CreateSaleItemDTO>
    {
        // Repositorio específico de ítems de venta para consultas de tickets/suscripciones y filtros por venta
        private readonly ISaleItemRepository _saleItemRepository;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="SaleItemController"/>.
        /// </summary>
        /// <param name="repository">Repositorio específico de ítems de venta.</param>
        /// <param name="mapper">Servicio de mapeo entre entidades y DTOs.</param>
        /// <param name="logger">Logger del controlador.</param>
        public SaleItemController(ISaleItemRepository repository, IMapper mapper, ILogger<SaleItemController> logger)
            : base(repository, mapper, logger)
        {
            _saleItemRepository = repository;
        }

        /// <summary>
        /// Obtiene el listado completo de ítems de venta.
        /// </summary>
        /// <returns>Listado de ítems de venta.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public override async Task<IActionResult> GetAll()
        {
            var entities = await _saleItemRepository.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<SaleItemDTO>>(entities);

            return Ok(BuildOk(dtos, HttpStatusCode.OK));
        }

        /// <summary>
        /// Obtiene un ítem de venta por su identificador.
        /// </summary>
        /// <param name="id">Identificador del ítem de venta.</param>
        /// <returns>Ítem de venta solicitado.</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public override async Task<IActionResult> Get(int id)
        {
            var entity = await _saleItemRepository.GetAsync(id);
            if (entity == null)
            {
                return NotFound(BuildFail(
                    HttpStatusCode.NotFound,
                    new[] { "Registro no encontrado." }
                ));
            }

            var dto = _mapper.Map<SaleItemDTO>(entity);
            if (dto == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    BuildFail(
                        HttpStatusCode.InternalServerError,
                        new[] { "No se pudo generar el DTO del ítem de venta." }
                    )
                );
            }

            return Ok(BuildOk(dto, HttpStatusCode.OK));
        }

        /// <summary>
        /// Obtiene los ítems de venta correspondientes a tickets.
        /// </summary>
        /// <returns>Listado de ítems de venta de tipo ticket.</returns>
        [HttpGet("tickets")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTickets()
        {
            var entities = await _saleItemRepository.GetTicketsAsync();
            var dtos = _mapper.Map<IEnumerable<SaleItemDTO>>(entities);

            return Ok(BuildOk(dtos, HttpStatusCode.OK));
        }

        /// <summary>
        /// Obtiene los ítems de venta correspondientes a suscripciones.
        /// </summary>
        /// <returns>Listado de ítems de venta de tipo suscripción.</returns>
        [HttpGet("subscriptions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSubscriptions()
        {
            var entities = await _saleItemRepository.GetSubscriptionsAsync();
            var dtos = _mapper.Map<IEnumerable<SaleItemDTO>>(entities);

            return Ok(BuildOk(dtos, HttpStatusCode.OK));
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
            var entities = await _saleItemRepository.GetBySaleAsync(saleId);
            if (entities == null || !entities.Any())
            {
                return NotFound(BuildFail(
                    HttpStatusCode.NotFound,
                    new[] { $"No se encontraron ítems para la venta con ID {saleId}." }
                ));
            }

            var dtos = _mapper.Map<IEnumerable<SaleItemDTO>>(entities);

            return Ok(BuildOk(dtos, HttpStatusCode.OK));
        }

    }
}
