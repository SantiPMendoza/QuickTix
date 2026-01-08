using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickTix.Contracts.Common;
using QuickTix.Contracts.Models.DTOs;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using System.Net;

namespace QuickTix.API.Controllers
{
    /// <summary>
    /// Controlador API para la gestión de managers y su vinculación con Identity.
    /// Incluye creación/actualización del AppUser asociado, asignación del rol "manager" y asociación a un Venue.
    /// </summary>
    [Authorize(Roles = "admin,manager")]
    [Route("api/[controller]")]
    [ApiController]
    public class ManagerController : BaseController<Manager, ManagerDTO, CreateManagerDTO>
    {
        // Gestor de usuarios Identity para crear/actualizar el AppUser asociado al Manager
        private readonly UserManager<AppUser> _userManager;

        // Gestor de roles Identity para asegurar y asignar el rol "manager"
        private readonly RoleManager<IdentityRole> _roleManager;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="ManagerController"/>.
        /// </summary>
        /// <param name="repository">Repositorio de managers.</param>
        /// <param name="mapper">Servicio de mapeo entre entidades y DTOs.</param>
        /// <param name="logger">Logger del controlador.</param>
        /// <param name="userManager">Gestor de usuarios Identity.</param>
        /// <param name="roleManager">Gestor de roles Identity.</param>
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

        /// <summary>
        /// Crea un manager y su AppUser asociado en Identity, asignándole el rol "manager".
        /// Valida duplicados por NIF/NIE y teléfono antes de persistir.
        /// </summary>
        /// <param name="dto">Datos de creación del manager.</param>
        /// <returns>Manager creado.</returns>
        [HttpPost]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public override async Task<IActionResult> Create([FromBody] CreateManagerDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(BuildFail(
                    HttpStatusCode.BadRequest,
                    ExtractModelStateErrors(ModelState)
                ));
            }

            var normalizedNif = string.IsNullOrWhiteSpace(dto.Nif) ? null : dto.Nif.Trim().ToUpperInvariant();
            var normalizedPhone = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();

            if (normalizedNif != null)
            {
                var nifExists = await _userManager.Users.AnyAsync(u => u.Nif == normalizedNif);
                if (nifExists)
                {
                    return Conflict(BuildFail(
                        HttpStatusCode.Conflict,
                        new[] { "Ya existe un usuario con ese NIF/NIE." }
                    ));
                }
            }

            if (normalizedPhone != null)
            {
                var phoneExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == normalizedPhone);
                if (phoneExists)
                {
                    return Conflict(BuildFail(
                        HttpStatusCode.Conflict,
                        new[] { "Ya existe un usuario con ese número de teléfono." }
                    ));
                }
            }

            // 1) Crear AppUser asociado a este Manager
            var appUser = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Name = dto.Name,
                Nif = normalizedNif,
                PhoneNumber = normalizedPhone,
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

            var createdResponse = BuildOk(responseDto, HttpStatusCode.Created);

            return CreatedAtAction(nameof(Get), new { id = responseDto.Id }, createdResponse);
        }

        /// <summary>
        /// Actualiza un manager y su AppUser asociado en Identity.
        /// Valida duplicados por NIF/NIE y teléfono excluyendo el usuario actual.
        /// </summary>
        /// <param name="id">Identificador del manager.</param>
        /// <param name="dto">Datos actualizados del manager.</param>
        /// <returns>Manager actualizado.</returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public override async Task<IActionResult> Update(int id, [FromBody] ManagerDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(BuildFail(
                    HttpStatusCode.BadRequest,
                    ExtractModelStateErrors(ModelState)
                ));
            }

            var manager = await _repository.GetForUpdateAsync(id);
            if (manager == null)
            {
                return NotFound(BuildFail(
                    HttpStatusCode.NotFound,
                    new[] { "Registro no encontrado." }
                ));
            }

            if (manager.AppUser == null)
                throw new InvalidOperationException("No se encontró el usuario asociado al gestor.");

            var normalizedNif = string.IsNullOrWhiteSpace(dto.Nif) ? null : dto.Nif.Trim().ToUpperInvariant();
            var normalizedPhone = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();

            var currentUserId = manager.AppUser.Id;

            if (normalizedNif != null)
            {
                var nifExists = await _userManager.Users.AnyAsync(u => u.Nif == normalizedNif && u.Id != currentUserId);
                if (nifExists)
                {
                    return Conflict(BuildFail(
                        HttpStatusCode.Conflict,
                        new[] { "Ya existe un usuario con ese NIF/NIE." }
                    ));
                }
            }

            if (normalizedPhone != null)
            {
                var phoneExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == normalizedPhone && u.Id != currentUserId);
                if (phoneExists)
                {
                    return Conflict(BuildFail(
                        HttpStatusCode.Conflict,
                        new[] { "Ya existe un usuario con ese número de teléfono." }
                    ));
                }
            }

            manager.AppUser.Name = dto.Name;
            manager.AppUser.Email = dto.Email;
            manager.AppUser.UserName = dto.Email;
            manager.AppUser.Nif = normalizedNif;
            manager.AppUser.PhoneNumber = normalizedPhone;

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

            return Ok(BuildOk(updatedDto, HttpStatusCode.OK));
        }
    }
}
