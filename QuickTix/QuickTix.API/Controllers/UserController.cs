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
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UserController(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        // --------------------------------------------------------------------
        // GET: api/User
        // Flujo:
        //  1) Obtener lista DTO desde repositorio.
        //  2) Devolver ApiResponse<List<UserDTO>>.
        //  3) Errores -> excepciones -> ApiExceptionFilter.
        // --------------------------------------------------------------------
        [HttpGet]
        [Authorize] // Ajusta roles cuando lo tengas claro
        public async Task<IActionResult> GetUsersAsync()
        {
            var userListDto = await _userRepository.GetUserDTOsAsync();

            var response = new ApiResponse<List<UserDTO>>
            {
                StatusCode = HttpStatusCode.OK,
                IsSuccess = true,
                Result = userListDto
            };

            return Ok(response);
        }

        // --------------------------------------------------------------------
        // GET: api/User/{id}
        // Mejoras:
        //  - En tu código original faltaba await al llamar al repositorio.
        //  - Si no existe, lanzamos KeyNotFoundException -> 404 por filtro.
        // --------------------------------------------------------------------
        [HttpGet("{id}", Name = "GetUser")]
        [Authorize]
        public async Task<IActionResult> GetUserAsync(string id)
        {
            var user = await _userRepository.GetUserAsync(id);
            if (user == null)
                throw new KeyNotFoundException("Usuario no encontrado.");

            var userDto = _mapper.Map<UserDTO>(user);

            var response = new ApiResponse<UserDTO>
            {
                StatusCode = HttpStatusCode.OK,
                IsSuccess = true,
                Result = userDto
            };

            return Ok(response);
        }

        // --------------------------------------------------------------------
        // POST: api/User/register
        // Flujo:
        //  1) Validar ModelState.
        //  2) Validar unicidad.
        //  3) Registrar.
        //  4) Devolver ApiResponse<...>.
        //
        // Nota:
        //  - En errores de validación devolvemos BadRequest directamente
        //    (esto es un error "de forma", no una excepción).
        // --------------------------------------------------------------------
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] UserRegistrationDTO registrationDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiErrorResponse { Message = "Entrada inválida." });

            var isUnique = await _userRepository.IsUniqueUserAsync(registrationDto.UserName);
            if (!isUnique)
                throw new ArgumentException("El nombre de usuario ya existe.");

            var newUser = await _userRepository.RegisterAsync(registrationDto);
            if (newUser == null)
                throw new InvalidOperationException("Error registrando el usuario.");

            var response = new ApiResponse<object>
            {
                StatusCode = HttpStatusCode.OK,
                IsSuccess = true,
                Result = newUser
            };

            return Ok(response);
        }

        // --------------------------------------------------------------------
        // POST: api/User/login
        // Mejora recomendada:
        //  - Credenciales incorrectas deberían ser 401 (Unauthorized),
        //    no 400 (BadRequest). Esto también ayuda a clientes.
        // --------------------------------------------------------------------
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] UserLoginDTO loginDto)
        {
            var responseLogin = await _userRepository.LoginAsync(loginDto);

            if (responseLogin == null || responseLogin.User == null || string.IsNullOrWhiteSpace(responseLogin.Token))
                throw new UnauthorizedAccessException("Usuario o contraseña incorrectos.");

            var response = new ApiResponse<UserLoginResponseDTO>
            {
                StatusCode = HttpStatusCode.OK,
                IsSuccess = true,
                Result = responseLogin
            };

            return Ok(response);
        }

        // --------------------------------------------------------------------
        // POST: api/User/change-password
        // Flujo:
        //  1) Extraer UserId del token (fallback de claims).
        //  2) Ejecutar cambio de contraseña en repositorio.
        //  3) Devolver ApiResponse<bool>.
        //
        // Mejora:
        //  - Eliminamos debug claims en respuesta; si quieres trazas, usa ILogger.
        // --------------------------------------------------------------------
        [Authorize]
        [HttpPost("change-password")]
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

            var response = new ApiResponse<bool>
            {
                StatusCode = HttpStatusCode.OK,
                IsSuccess = true,
                Result = true
            };

            return Ok(response);
        }
    }
}
