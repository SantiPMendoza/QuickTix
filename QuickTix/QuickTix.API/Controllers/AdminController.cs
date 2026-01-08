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
    /// Controlador API para la gestión de administradores y su vinculación con Identity.
    /// Incluye la creación/actualización del AppUser asociado y la asignación del rol "admin".
    /// </summary>
    [Authorize(Roles = "admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : BaseController<Admin, AdminDTO, CreateAdminDTO>
    {
        // Gestor de usuarios Identity para crear/actualizar el AppUser asociado al Admin
        private readonly UserManager<AppUser> _userManager;

        // Gestor de roles Identity para asegurar y asignar el rol "admin"
        private readonly RoleManager<IdentityRole> _roleManager;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="AdminController"/>.
        /// </summary>
        /// <param name="repository">Repositorio de administradores.</param>
        /// <param name="mapper">Servicio de mapeo entre entidades y DTOs.</param>
        /// <param name="logger">Logger del controlador.</param>
        /// <param name="userManager">Gestor de usuarios Identity.</param>
        /// <param name="roleManager">Gestor de roles Identity.</param>
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

        /// <summary>
        /// Crea un administrador y su AppUser asociado en Identity, asignándole el rol "admin".
        /// Valida duplicados por NIF/NIE y teléfono antes de persistir.
        /// </summary>
        /// <param name="dto">Datos de creación del administrador.</param>
        /// <returns>Administrador creado.</returns>
        [HttpPost]
        [Authorize(Roles = "admin")]
        public override async Task<IActionResult> Create([FromBody] CreateAdminDTO dto)
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

            // 1) Crear AppUser asociado al administrador
            var appUser = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Name = dto.Name,
                Nif = dto.Nif,
                PhoneNumber = dto.PhoneNumber,
                MustChangePassword = true
            };

            var result = await _userManager.CreateAsync(appUser, $"{dto.Nif}+*");
            if (!result.Succeeded)
            {
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
            var responseDto = _mapper.Map<AdminDTO>(admin);

            var createdResponse = BuildOk(responseDto, HttpStatusCode.Created);

            return CreatedAtAction(nameof(Get), new { id = responseDto.Id }, createdResponse);
        }

        /// <summary>
        /// Actualiza un administrador y su AppUser asociado en Identity.
        /// Valida duplicados por NIF/NIE y teléfono excluyendo el usuario actual.
        /// </summary>
        /// <param name="id">Identificador del administrador.</param>
        /// <param name="dto">Datos actualizados del administrador.</param>
        /// <returns>Administrador actualizado.</returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "admin")]
        public override async Task<IActionResult> Update(int id, [FromBody] AdminDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(BuildFail(
                    HttpStatusCode.BadRequest,
                    ExtractModelStateErrors(ModelState)
                ));
            }

            var admin = await _repository.GetForUpdateAsync(id);
            if (admin == null)
            {
                return NotFound(BuildFail(
                    HttpStatusCode.NotFound,
                    new[] { "Registro no encontrado." }
                ));
            }

            if (admin.AppUser == null)
                throw new InvalidOperationException("No se encontró el usuario asociado al administrador.");

            var normalizedNif = string.IsNullOrWhiteSpace(dto.Nif) ? null : dto.Nif.Trim().ToUpperInvariant();
            var normalizedPhone = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();

            var currentUserId = admin.AppUser.Id;

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

            admin.AppUser.Name = dto.Name;
            admin.AppUser.Email = dto.Email;
            admin.AppUser.UserName = dto.Email;
            admin.AppUser.Nif = dto.Nif;
            admin.AppUser.PhoneNumber = dto.PhoneNumber;

            var userUpdateResult = await _userManager.UpdateAsync(admin.AppUser);
            if (!userUpdateResult.Succeeded)
            {
                var errors = string.Join(" | ", userUpdateResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Error Identity al actualizar AppUser del Admin: {errors}");
            }

            admin.Name = dto.Name;
            await _repository.UpdateAsync(admin);

            var updatedDto = _mapper.Map<AdminDTO>(admin);

            return Ok(BuildOk(updatedDto, HttpStatusCode.OK));
        }
    }
}
