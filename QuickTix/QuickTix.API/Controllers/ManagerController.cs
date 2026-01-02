using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuickTix.Contracts.Common;
using QuickTix.Core.Interfaces;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Core.Models.Entities;
using System.Net;

namespace QuickTix.API.Controllers
{
    // Recomendación: evitar AllowAnonymous aquí si quieres que Authorize funcione.
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
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public override async Task<IActionResult> Create([FromBody] CreateManagerDTO dto)
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

            // 1) Crear AppUser asociado a este Manager
            var appUser = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Name = dto.Name,
                Nif = dto.Nif,
                PhoneNumber = dto.PhoneNumber,
                MustChangePassword = true
            };

            // Alta en Identity con la contraseña por defecto basada en el NIF
            var result = await _userManager.CreateAsync(appUser, $"{dto.Nif}*");
            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Error Identity al crear AppUser del Manager: {errors}");
            }

            // 2) Asegurar que existe el rol "manager"
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

            // 3) Asignar el rol "manager"
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
            var responseDto = _mapper.Map<ManagerDTO>(manager);

            var createdResponse = ApiResponse<ManagerDTO>.Ok(responseDto, HttpStatusCode.Created, traceId);

            // Usa la acción Get(int id) heredada del BaseController
            return CreatedAtAction(nameof(Get), new { id = responseDto.Id }, createdResponse);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public override async Task<IActionResult> Update(int id, [FromBody] ManagerDTO dto)
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

            var manager = await _repository.GetForUpdateAsync(id);
            if (manager == null)
            {
                return NotFound(ApiResponse<object>.Fail(
                    HttpStatusCode.NotFound,
                    new[] { "Registro no encontrado." },
                    traceId
                ));
            }

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

            var updatedDto = _mapper.Map<ManagerDTO>(manager);

            return Ok(ApiResponse<ManagerDTO>.Ok(updatedDto, HttpStatusCode.OK, traceId));
        }
    }
}
