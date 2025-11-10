using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.DTOs;
using QuickTix.Core.Models.Entities;


namespace QuickTix.API.Controllers
{
    //[Authorize(Roles = "admin")]
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : BaseController<Admin, AdminDTO, CreateAdminDTO>
    {
        public AdminController(IAdminRepository repository, IMapper mapper, ILogger<AdminController> logger)
            : base(repository, mapper, logger)
        {
        }


        [HttpPost]
        [Authorize(Roles = "admin")]
        public override async Task<IActionResult> Create([FromBody] CreateAdminDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1️⃣ Crear AppUser asociado
            var appUser = new AppUser
            {
                UserName = dto.Nif,
                Email = dto.Email,
                Name = dto.Name,
                Nif = dto.Nif,

                MustChangePassword = true // fuerza cambio en el primer inicio de sesión
            };

            var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
            var result = await userManager.CreateAsync(appUser, $"{dto.Nif}+*");

            if (!result.Succeeded)
                throw new InvalidOperationException("No se pudo crear el usuario asociado al administrador.");

            // 2️⃣ Crear Manager vinculado al AppUser
            var admin = new Admin
            {
                Name = dto.Name,
                AppUserId = appUser.Id,
            };

            await _repository.CreateAsync(admin);

            // 3️⃣ Mapear a DTO para respuesta
            var response = _mapper.Map<AdminDTO>(admin);

            return CreatedAtRoute(
                $"{ControllerContext.ActionDescriptor.ControllerName}_GetEntity",
                new { id = response.Id },
                response);
        }
    }
}
