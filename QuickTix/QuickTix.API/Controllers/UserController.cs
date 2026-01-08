using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickTix.Contracts.Common;
using QuickTix.Contracts.DTOs.UserAuthDTO;
using QuickTix.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;

namespace QuickTix.API.Controllers
{
    /// <summary>
    /// Controlador API para operaciones de usuarios e identidad.
    /// Gestiona consulta de usuarios, registro, login y cambio de contraseña.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        // Repositorio de usuarios con operaciones de autenticación y gestión de credenciales
        private readonly IUserRepository _userRepository;

        // Mapper para convertir entidades de usuario a DTOs de salida
        private readonly IMapper _mapper;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="UserController"/>.
        /// </summary>
        /// <param name="userRepository">Repositorio de usuarios.</param>
        /// <param name="mapper">Servicio de mapeo entre entidades y DTOs.</param>
        public UserController(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// Obtiene el listado de usuarios en formato DTO.
        /// </summary>
        /// <returns>Listado de usuarios.</returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsersAsync()
        {
            var traceId = HttpContext.TraceIdentifier;

            var userListDto = await _userRepository.GetUserDTOsAsync();

            return Ok(ApiResponse<List<UserDTO>>.Ok(userListDto, HttpStatusCode.OK, traceId));
        }

        /// <summary>
        /// Obtiene un usuario por su identificador.
        /// </summary>
        /// <param name="id">Identificador del usuario.</param>
        /// <returns>Usuario solicitado.</returns>
        [HttpGet("{id}", Name = "GetUser")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserAsync(string id)
        {
            var user = await _userRepository.GetUserAsync(id);
            if (user == null)
                throw new KeyNotFoundException("Usuario no encontrado.");

            var userDto = _mapper.Map<UserDTO>(user);

            var traceId = HttpContext.TraceIdentifier;
            return Ok(ApiResponse<UserDTO>.Ok(userDto, HttpStatusCode.OK, traceId));
        }

        /// <summary>
        /// Registra un nuevo usuario en el sistema.
        /// </summary>
        /// <param name="registrationDto">Datos de registro.</param>
        /// <returns>Resultado del registro.</returns>
        [AllowAnonymous]
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterAsync([FromBody] UserRegistrationDTO registrationDto)
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

            var isUnique = await _userRepository.IsUniqueUserAsync(registrationDto.UserName);
            if (!isUnique)
                throw new ArgumentException("El nombre de usuario ya existe.");

            var newUser = await _userRepository.RegisterAsync(registrationDto);
            if (newUser == null)
                throw new InvalidOperationException("Error registrando el usuario.");

            return Ok(ApiResponse<object>.Ok(newUser, HttpStatusCode.OK, traceId));
        }

        /// <summary>
        /// Autentica un usuario y devuelve el token de acceso.
        /// </summary>
        /// <param name="loginDto">Credenciales de acceso.</param>
        /// <returns>Usuario autenticado y token.</returns>
        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LoginAsync([FromBody] UserLoginDTO loginDto)
        {
            var traceId = HttpContext.TraceIdentifier;

            var responseLogin = await _userRepository.LoginAsync(loginDto);

            if (responseLogin == null || responseLogin.User == null || string.IsNullOrWhiteSpace(responseLogin.Token))
                throw new UnauthorizedAccessException("Usuario o contraseña incorrectos.");

            return Ok(ApiResponse<UserLoginResponseDTO>.Ok(responseLogin, HttpStatusCode.OK, traceId));
        }

        /// <summary>
        /// Cambia la contraseña del usuario autenticado.
        /// </summary>
        /// <param name="dto">Contraseña actual y nueva contraseña.</param>
        /// <returns>Resultado de la operación.</returns>
        [Authorize]
        [HttpPost("change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequestDTO dto)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                User.FindFirstValue("nameid") ??
                User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("Token inválido o sin identificador de usuario.");

            await _userRepository.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);

            var traceId = HttpContext.TraceIdentifier;
            return Ok(ApiResponse<bool>.Ok(true, HttpStatusCode.OK, traceId));
        }
    }
}
