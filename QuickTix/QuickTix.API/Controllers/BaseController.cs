using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using QuickTix.Contracts.Common;
using QuickTix.Core.Interfaces;
using System.Net;

namespace QuickTix.API.Controllers
{
    /// <summary>
    /// Controlador base genérico para exponer endpoints CRUD reutilizables.
    /// Centraliza el uso de IRepository, AutoMapper y el contrato ApiResponse para respuestas consistentes.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController<TEntity, TDto, TCreateDto> : ControllerBase
        where TEntity : class
    {
        // Repositorio genérico para operaciones CRUD de la entidad
        protected readonly IRepository<TEntity> _repository;

        // Mapper para conversión entre entidad y DTOs
        protected readonly IMapper _mapper;

        // Logger del controlador derivado
        protected readonly ILogger _logger;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="BaseController{TEntity, TDto, TCreateDto}"/>.
        /// </summary>
        /// <param name="repository">Repositorio genérico de la entidad.</param>
        /// <param name="mapper">Servicio de mapeo entre entidades y DTOs.</param>
        /// <param name="logger">Logger del controlador derivado.</param>
        protected BaseController(IRepository<TEntity> repository, IMapper mapper, ILogger logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        // Identificador de trazabilidad por petición, propagado en ApiResponse
        protected string TraceId => HttpContext.TraceIdentifier;

        /// <summary>
        /// Construye una respuesta ApiResponse de éxito con el resultado y el TraceId de la petición.
        /// </summary>
        /// <typeparam name="T">Tipo del payload de salida.</typeparam>
        /// <param name="result">Resultado a devolver.</param>
        /// <param name="statusCode">Código HTTP lógico asociado al éxito.</param>
        /// <returns>ApiResponse de éxito.</returns>
        protected ApiResponse<T> BuildOk<T>(T result, HttpStatusCode statusCode = HttpStatusCode.OK)
            => ApiResponse<T>.Ok(result, statusCode, TraceId);

        /// <summary>
        /// Construye una respuesta ApiResponse de error con los mensajes y el TraceId de la petición.
        /// </summary>
        /// <param name="statusCode">Código HTTP del error.</param>
        /// <param name="errors">Listado de errores a exponer.</param>
        /// <returns>ApiResponse de error.</returns>
        protected ApiResponse<object> BuildFail(HttpStatusCode statusCode, IEnumerable<string> errors)
            => ApiResponse<object>.Fail(statusCode, errors, TraceId);

        /// <summary>
        /// Extrae y normaliza los errores de validación del ModelState.
        /// </summary>
        /// <param name="modelState">Estado del modelo recibido en la petición.</param>
        /// <returns>Listado de mensajes de error.</returns>
        protected static List<string> ExtractModelStateErrors(ModelStateDictionary modelState)
        {
            var errors = modelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Error de validación." : e.ErrorMessage)
                .ToList();

            return errors.Count == 0 ? new List<string> { "Error de validación." } : errors;
        }

        /// <summary>
        /// Obtiene el listado completo de registros de la entidad.
        /// </summary>
        /// <returns>Listado de DTOs.</returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public virtual async Task<IActionResult> GetAll()
        {
            var entities = await _repository.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<TDto>>(entities);

            return Ok(BuildOk(dtos));
        }

        /// <summary>
        /// Obtiene un registro por su identificador.
        /// </summary>
        /// <param name="id">Identificador del registro.</param>
        /// <returns>DTO del registro solicitado.</returns>
        [HttpGet("{id:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> Get(int id)
        {
            var entity = await _repository.GetAsync(id);
            if (entity == null)
            {
                return NotFound(BuildFail(
                    HttpStatusCode.NotFound,
                    new[] { "Registro no encontrado." }
                ));
            }

            var dto = _mapper.Map<TDto>(entity);
            return Ok(BuildOk(dto));
        }

        /// <summary>
        /// Crea un nuevo registro a partir del DTO de creación.
        /// </summary>
        /// <param name="createDto">DTO con los datos de creación.</param>
        /// <returns>DTO del registro creado.</returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public virtual async Task<IActionResult> Create([FromBody] TCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(BuildFail(
                    HttpStatusCode.BadRequest,
                    ExtractModelStateErrors(ModelState)
                ));
            }

            var entity = _mapper.Map<TEntity>(createDto);
            await _repository.CreateAsync(entity);

            var dto = _mapper.Map<TDto>(entity);

            var createdResponse = BuildOk(dto, HttpStatusCode.Created);

            var idValue = dto?.GetType().GetProperty("Id")?.GetValue(dto);
            if (idValue == null)
            {
                return StatusCode(StatusCodes.Status201Created, createdResponse);
            }

            return CreatedAtAction(nameof(Get), new { id = idValue }, createdResponse);
        }

        /// <summary>
        /// Actualiza un registro existente a partir de su identificador y DTO.
        /// </summary>
        /// <param name="id">Identificador del registro.</param>
        /// <param name="dto">DTO con los datos actualizados.</param>
        /// <returns>DTO del registro actualizado.</returns>
        [HttpPut("{id:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> Update(int id, [FromBody] TDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(BuildFail(
                    HttpStatusCode.BadRequest,
                    ExtractModelStateErrors(ModelState)
                ));
            }

            var entity = await _repository.GetAsync(id);
            if (entity == null)
            {
                return NotFound(BuildFail(
                    HttpStatusCode.NotFound,
                    new[] { "Registro no encontrado." }
                ));
            }

            _mapper.Map(dto, entity);
            await _repository.UpdateAsync(entity);

            var updatedDto = _mapper.Map<TDto>(entity);
            return Ok(BuildOk(updatedDto));
        }

        /// <summary>
        /// Elimina un registro por su identificador.
        /// </summary>
        /// <param name="id">Identificador del registro.</param>
        /// <returns>Respuesta de éxito sin payload.</returns>
        [HttpDelete("{id:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> Delete(int id)
        {
            var entity = await _repository.GetAsync(id);
            if (entity == null)
            {
                return NotFound(BuildFail(
                    HttpStatusCode.NotFound,
                    new[] { "Registro no encontrado." }
                ));
            }

            await _repository.DeleteAsync(id);

            return Ok(BuildOk<object?>(null));
        }
    }
}
