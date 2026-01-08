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
    [Authorize(Roles = "admin")]
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
            var traceId = HttpContext.TraceIdentifier;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Error de validación." : e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.Fail(HttpStatusCode.BadRequest, errors, traceId));
            }

            var normalizedNif = string.IsNullOrWhiteSpace(dto.Nif) ? null : dto.Nif.Trim().ToUpperInvariant();
            var normalizedPhone = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();

            if (normalizedNif != null)
            {
                var nifExists = await _userManager.Users.AnyAsync(u => u.Nif == normalizedNif);
                if (nifExists)
                    return Conflict(ApiResponse<object>.Fail(HttpStatusCode.Conflict, new[] { "Ya existe un usuario con ese NIF/NIE." }, traceId));
            }

            if (normalizedPhone != null)
            {
                var phoneExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == normalizedPhone);
                if (phoneExists)
                    return Conflict(ApiResponse<object>.Fail(HttpStatusCode.Conflict, new[] { "Ya existe un usuario con ese número de teléfono." }, traceId));
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

            var createdResponse = ApiResponse<AdminDTO>.Ok(responseDto, HttpStatusCode.Created, traceId);

            return CreatedAtAction(nameof(Get), new { id = responseDto.Id }, createdResponse);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "admin")]
        public override async Task<IActionResult> Update(int id, [FromBody] AdminDTO dto)
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

            var admin = await _repository.GetForUpdateAsync(id);
            if (admin == null)
            {
                return NotFound(ApiResponse<object>.Fail(
                    HttpStatusCode.NotFound,
                    new[] { "Registro no encontrado." },
                    traceId
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
                    return Conflict(ApiResponse<object>.Fail(HttpStatusCode.Conflict, new[] { "Ya existe un usuario con ese NIF/NIE." }, traceId));
            }

            if (normalizedPhone != null)
            {
                var phoneExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == normalizedPhone && u.Id != currentUserId);
                if (phoneExists)
                    return Conflict(ApiResponse<object>.Fail(HttpStatusCode.Conflict, new[] { "Ya existe un usuario con ese número de teléfono." }, traceId));
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

            return Ok(ApiResponse<AdminDTO>.Ok(updatedDto, HttpStatusCode.OK, traceId));
        }
    }
}
