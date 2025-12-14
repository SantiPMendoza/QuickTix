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
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ManagerController(
            IManagerRepository repository,
            IMapper mapper,
            ILogger<ManagerController> logger,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
            : base(repository, mapper, logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public override async Task<IActionResult> Create([FromBody] CreateManagerDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1) Crear AppUser asociado a este Manager
            var appUser = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Name = dto.Name,
                Nif = dto.Nif,
                PhoneNumber = dto.PhoneNumber,
                MustChangePassword = true // fuerza cambio en el primer inicio de sesión
            };

            // Alta en Identity con la contraseña por defecto basada en el NIF
            var result = await _userManager.CreateAsync(appUser, $"{dto.Nif}+*");
            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Error Identity al crear AppUser del Manager: {errors}");
            }

            // 2) Asegurar que existe el rol "manager" en Identity
            const string managerRole = "manager";
            if (!await _roleManager.RoleExistsAsync(managerRole))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole(managerRole));
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(" | ", roleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    throw new InvalidOperationException($"Error Identity al crear rol '{managerRole}': {errors}");
                }
            }

            // 3) Asignar el rol "manager" al nuevo AppUser
            // Comentario: esta relación se guarda en AspNetUserRoles; luego GetRolesAsync(user) devolverá "manager".
            var addToRoleResult = await _userManager.AddToRoleAsync(appUser, managerRole);
            if (!addToRoleResult.Succeeded)
            {
                var errors = string.Join(" | ", addToRoleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Error Identity al asignar rol '{managerRole}' al usuario: {errors}");
            }

            // 4) Crear la entidad Manager vinculada al AppUser
            var manager = new Manager
            {
                Name = dto.Name,
                AppUserId = appUser.Id,
                VenueId = dto.VenueId
            };

            await _repository.CreateAsync(manager);

            // 5) Mapear a DTO para respuesta
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

            var manager = await _repository.GetForUpdateAsync(id);
            if (manager == null)
                return NotFound();

            if (manager.AppUser == null)
                throw new InvalidOperationException("No se encontró el usuario asociado al gestor.");

            manager.AppUser.Name = dto.Name;
            manager.AppUser.Email = dto.Email;
            manager.AppUser.UserName = dto.Email;
            manager.AppUser.Nif = dto.Nif;
            manager.AppUser.PhoneNumber = dto.PhoneNumber;

            var userUpdateResult = await _userManager.UpdateAsync(manager.AppUser);
            if (!userUpdateResult.Succeeded)
            {
                var errors = string.Join(" | ", userUpdateResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Error Identity al actualizar AppUser del Manager: {errors}");
            }

            manager.Name = dto.Name;
            manager.VenueId = dto.VenueId;

            await _repository.UpdateAsync(manager);

            return Ok(_mapper.Map<ManagerDTO>(manager));
        }

    }
}
