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
    [AllowAnonymous] //[Authorize(Roles = "admin,manager")]
    [Route("api/[controller]")]
    [ApiController]
    public class ManagerController : BaseController<Manager, ManagerDTO, CreateManagerDTO>
    {
        public ManagerController(IManagerRepository repository, IMapper mapper, ILogger<ManagerController> logger)
            : base(repository, mapper, logger)
        {


        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public override async Task<IActionResult> Create([FromBody] CreateManagerDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1️⃣ Crear AppUser asociado
            var appUser = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Name = dto.Name,
                Nif = dto.Nif,

                MustChangePassword = true // fuerza cambio en el primer inicio de sesión
            };

            var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
            var result = await userManager.CreateAsync(appUser, $"{dto.Nif}+*");

            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Error Identity: {errors}");
            }


            // 2️⃣ Crear Manager vinculado al AppUser
            var manager = new Manager
            {
                Name = dto.Name,
                AppUserId = appUser.Id,
                VenueId = dto.VenueId
            };

            await _repository.CreateAsync(manager);

            // 3️⃣ Mapear a DTO para respuesta
            var response = _mapper.Map<ManagerDTO>(manager);

            return CreatedAtRoute(
                $"{ControllerContext.ActionDescriptor.ControllerName}_GetEntity",
                new { id = response.Id },
                response);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "admin")]
        public override async Task<IActionResult> Update(int id, [FromBody] ManagerDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var manager = await _repository.GetAsync(id);
            if (manager == null)
                return NotFound();

            var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
            var appUser = await userManager.FindByIdAsync(manager.AppUserId);

            if (appUser == null)
                throw new InvalidOperationException("No se encontró el usuario asociado al gestor.");

            // 🔹 Actualizar AppUser
            appUser.Name = dto.Name;
            appUser.Email = dto.Email;
            appUser.UserName = dto.Email;
            appUser.Nif = dto.Nif;
            appUser.PhoneNumber = dto.PhoneNumber;

            var result = await userManager.UpdateAsync(appUser);
            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Error Identity: {errors}");
            }

            // 🔹 Actualizar Manager
            manager.Name = dto.Name;
            manager.VenueId = dto.VenueId;

            await _repository.UpdateAsync(manager);

            var updated = _mapper.Map<ManagerDTO>(manager);
            return Ok(updated);
        }




    }
}
