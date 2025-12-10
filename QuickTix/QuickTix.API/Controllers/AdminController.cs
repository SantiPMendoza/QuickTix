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
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            IAdminRepository repository,
            IMapper mapper,
            ILogger<AdminController> logger,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
            : base(repository, mapper, logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public override async Task<IActionResult> Create([FromBody] CreateAdminDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1) Crear AppUser asociado al administrador
            var appUser = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Name = dto.Name,
                Nif = dto.Nif,
                MustChangePassword = true
            };

            var result = await _userManager.CreateAsync(appUser, $"{dto.Nif}+*");
            if (!result.Succeeded)
            {
                // Se agregan todos los errores de Identity en un único mensaje para facilitar el diagnóstico
                var errors = string.Join(" | ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Error Identity al crear AppUser del Admin: {errors}");
            }

            // 2) Asegurar que existe el rol "admin"
            const string adminRole = "admin";
            if (!await _roleManager.RoleExistsAsync(adminRole))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole(adminRole));
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(" | ", roleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    throw new InvalidOperationException($"Error Identity al crear rol '{adminRole}': {errors}");
                }
            }

            // 3) Asignar el rol "admin" al usuario
            var addToRoleResult = await _userManager.AddToRoleAsync(appUser, adminRole);
            if (!addToRoleResult.Succeeded)
            {
                var errors = string.Join(" | ", addToRoleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Error Identity al asignar rol '{adminRole}' al usuario: {errors}");
            }

            // 4) Crear la entidad Admin vinculada al AppUser
            var admin = new Admin
            {
                Name = dto.Name,
                AppUserId = appUser.Id
            };

            await _repository.CreateAsync(admin);

            // 5) Mapear a DTO para respuesta
            var response = _mapper.Map<AdminDTO>(admin);

            return CreatedAtRoute(
                $"{ControllerContext.ActionDescriptor.ControllerName}_GetEntity",
                new { id = response.Id },
                response);
        }

        [HttpPut("{id:int}")]
        //[Authorize(Roles = "admin")]
        public override async Task<IActionResult> Update(int id, [FromBody] AdminDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var admin = await _repository.GetAsync(id);
            if (admin == null)
                return NotFound();

            // Se usa el UserManager inyectado, en lugar de resolverlo desde HttpContext
            var appUser = await _userManager.FindByIdAsync(admin.AppUserId);
            if (appUser == null)
                throw new InvalidOperationException("No se encontró el usuario asociado al administrador.");

            // Actualizar AppUser
            appUser.Name = dto.Name;
            appUser.Email = dto.Email;
            appUser.UserName = dto.Email;
            appUser.Nif = dto.Nif;
            appUser.PhoneNumber = dto.PhoneNumber;

            var userUpdateResult = await _userManager.UpdateAsync(appUser);
            if (!userUpdateResult.Succeeded)
            {
                var errors = string.Join(" | ",
                    userUpdateResult.Errors.Select(e => $"{e.Code}: {e.Description}"));

                throw new InvalidOperationException($"Error Identity al actualizar AppUser del Admin: {errors}");
            }

            // Actualizar entidad Admin
            admin.Name = dto.Name;

            await _repository.UpdateAsync(admin);

            var updated = _mapper.Map<AdminDTO>(admin);
            return Ok(updated);
        }
    }
}
