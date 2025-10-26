using Microsoft.AspNetCore.Mvc;
using QuickTix.Core.Models.DTOs.UserAuthDTO;
using QuickTix.Core.Interfaces;

namespace QuickTix.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepo;

        public AuthController(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegistrationDTO dto)
        {
            var result = await _userRepo.RegisterAsync(dto);
            if (result == null)
                return BadRequest("No se pudo registrar el usuario.");
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDTO dto)
        {
            var result = await _userRepo.LoginAsync(dto);
            if (result == null)
                return Unauthorized("Usuario o contraseña incorrectos.");
            return Ok(result);
        }
    }
}
