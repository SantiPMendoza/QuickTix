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
    /// Controlador base genérico para endpoints CRUD de QuickTix.
    ///
    /// Este controlador:
    /// - Define operaciones CRUD comunes (GET, GET by ID, POST, PUT, DELETE).
    /// - Utiliza IRepository y AutoMapper.
    /// - No maneja excepciones con try/catch: se delega en ApiExceptionFilter para respuestas consistentes.
    ///
    /// Todas las respuestas (éxito y error) siguen el contrato ApiResponse{T}.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController<TEntity, TDto, TCreateDto> : ControllerBase
        where TEntity : class
    {
        protected readonly IRepository<TEntity> _repository;
        protected readonly IMapper _mapper;
        protected readonly ILogger _logger;

        protected BaseController(IRepository<TEntity> repository, IMapper mapper, ILogger logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        protected string TraceId => HttpContext.TraceIdentifier;

        protected ApiResponse<T> BuildOk<T>(T result, HttpStatusCode statusCode = HttpStatusCode.OK)
            => ApiResponse<T>.Ok(result, statusCode, TraceId);

        protected ApiResponse<object> BuildFail(HttpStatusCode statusCode, IEnumerable<string> errors)
            => ApiResponse<object>.Fail(statusCode, errors, TraceId);

        protected static List<string> ExtractModelStateErrors(ModelStateDictionary modelState)
        {
            var errors = modelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Error de validación." : e.ErrorMessage)
                .ToList();

            return errors.Count == 0 ? new List<string> { "Error de validación." } : errors;
        }

        // GET: Obtener todos los registros
        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public virtual async Task<IActionResult> GetAll()
        {
            var entities = await _repository.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<TDto>>(entities);

            return Ok(BuildOk(dtos));
        }

        // GET: Obtener un registro por ID
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

        // POST: Crear nuevo registro
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

            var createdResponse = ApiResponse<TDto>.Ok(dto, HttpStatusCode.Created, TraceId);

            var idValue = dto?.GetType().GetProperty("Id")?.GetValue(dto);
            if (idValue == null)
            {
                return StatusCode(StatusCodes.Status201Created, createdResponse);
            }

            return CreatedAtAction(nameof(Get), new { id = idValue }, createdResponse);
        }

        // PUT: Actualizar registro existente
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

        // DELETE: Eliminar registro
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
