using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickTix.Core.Interfaces;
using System.Net;

namespace QuickTix.API.Controllers
{
    /// <summary>
    /// Controlador base genérico para todos los endpoints CRUD de QuickTix.
    /// 
    /// Este controlador:
    /// - Define las operaciones CRUD comunes (GET, GET by ID, POST, PUT, DELETE).
    /// - Utiliza inyección de dependencias para acceder al repositorio (<see cref="IRepository{TEntity}"/>) y AutoMapper.
    /// - NO maneja errores internamente mediante try/catch.
    /// 
    /// En su lugar, todas las excepciones (de lógica, validación o base de datos)
    /// son capturadas globalmente por el filtro <see cref="QuickTix.API.Filters.ApiExceptionFilter"/>,
    /// el cual convierte las excepciones en respuestas JSON uniformes con el formato:
    /// 
    ///     { "message": "Descripción del error" }
    /// 
    /// Esto permite que el cliente (WPF / MAUI) reciba errores consistentes,
    /// que son interpretados por <see cref="QuickTix.Desktop.Services.HttpJsonClient"/>
    /// y encapsulados como <see cref="QuickTix.Desktop.Services.ApiException"/>.
    /// 
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

        // ================================================================
        // 🔹 GET: Obtener todos los registros
        // ================================================================
        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public virtual async Task<IActionResult> GetAll()
        {
            var entities = await _repository.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<TDto>>(entities);
            return Ok(dtos);
        }

        // ================================================================
        // 🔹 GET: Obtener un registro por ID
        // ================================================================
        [HttpGet("{id:int}", Name = "[controller]_GetEntity")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> Get(int id)
        {
            var entity = await _repository.GetAsync(id);
            if (entity == null)
                return NotFound();

            var dto = _mapper.Map<TDto>(entity);
            return Ok(dto);
        }

        // ================================================================
        // 🔹 POST: Crear nuevo registro
        // ================================================================
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public virtual async Task<IActionResult> Create([FromBody] TCreateDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = _mapper.Map<TEntity>(createDto);
            await _repository.CreateAsync(entity);

            var dto = _mapper.Map<TDto>(entity);

            return CreatedAtRoute(
                $"{ControllerContext.ActionDescriptor.ControllerName}_GetEntity",
                new { id = dto.GetType().GetProperty("Id")?.GetValue(dto) },
                dto);
        }

        // ================================================================
        // 🔹 PUT: Actualizar registro existente
        // ================================================================
        [HttpPut("{id:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> Update(int id, [FromBody] TDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = await _repository.GetAsync(id);
            if (entity == null)
                return NotFound();

            _mapper.Map(dto, entity);
            await _repository.UpdateAsync(entity);

            return Ok(_mapper.Map<TDto>(entity));
        }

        // ================================================================
        // 🔹 DELETE: Eliminar registro
        // ================================================================
        [HttpDelete("{id:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> Delete(int id)
        {
            var entity = await _repository.GetAsync(id);
            if (entity == null)
                return NotFound();

            await _repository.DeleteAsync(id);
            return Ok();
        }
    }
}
