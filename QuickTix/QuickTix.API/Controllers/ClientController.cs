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
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ClientController : BaseController<Client, ClientDTO, CreateClientDTO>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ClientController(
            IClientRepository repository,
            IMapper mapper,
            ILogger<ClientController> logger,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
            : base(repository, mapper, logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public override async Task<IActionResult> Create([FromBody] CreateClientDTO dto)
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


            // 1) Crear AppUser asociado al cliente
            var appUser = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Name = dto.Name,
                Nif = normalizedNif,
                PhoneNumber = normalizedPhone,
                MustChangePassword = true
            };

            var result = await _userManager.CreateAsync(appUser, $"{dto.Nif}+*");
            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Error Identity al crear AppUser del Client: {errors}");
            }

            // 2) Asegurar que existe el rol "client"
            const string clientRole = "client";
            if (!await _roleManager.RoleExistsAsync(clientRole))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole(clientRole));
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(" | ", roleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    throw new InvalidOperationException($"Error Identity al crear rol '{clientRole}': {errors}");
                }
            }

            // 3) Asignar el rol "client" al usuario
            var addToRoleResult = await _userManager.AddToRoleAsync(appUser, clientRole);
            if (!addToRoleResult.Succeeded)
            {
                var errors = string.Join(" | ", addToRoleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Error Identity al asignar rol '{clientRole}' al usuario: {errors}");
            }

            // 4) Crear la entidad Client vinculada al AppUser
            var client = _mapper.Map<Client>(dto);
            client.AppUserId = appUser.Id;

            await _repository.CreateAsync(client);

            // 5) Mapear a DTO para respuesta
            var responseDto = _mapper.Map<ClientDTO>(client);

            var createdResponse = ApiResponse<ClientDTO>.Ok(responseDto, HttpStatusCode.Created, traceId);

            return CreatedAtAction(nameof(Get), new { id = responseDto.Id }, createdResponse);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "admin")]
        public override async Task<IActionResult> Update(int id, [FromBody] ClientDTO dto)
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



            var client = await _repository.GetForUpdateAsync(id);
            if (client == null)
            {
                return NotFound(ApiResponse<object>.Fail(
                    HttpStatusCode.NotFound,
                    new[] { "Registro no encontrado." },
                    traceId
                ));
            }

            if (client.AppUser == null)
                throw new InvalidOperationException("No se encontró el usuario asociado al cliente.");

            var normalizedNif = string.IsNullOrWhiteSpace(dto.Nif) ? null : dto.Nif.Trim().ToUpperInvariant();
            var normalizedPhone = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();

            var currentUserId = client.AppUser.Id;

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


            client.AppUser.Name = dto.Name;
            client.AppUser.Email = dto.Email;
            client.AppUser.UserName = dto.Email;
            client.AppUser.Nif = normalizedNif;
            client.AppUser.PhoneNumber = normalizedPhone;


            var userUpdateResult = await _userManager.UpdateAsync(client.AppUser);
            if (!userUpdateResult.Succeeded)
            {
                var errors = string.Join(" | ", userUpdateResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Error Identity al actualizar AppUser del Client: {errors}");
            }

            client.Name = dto.Name;
            await _repository.UpdateAsync(client);

            var updatedDto = _mapper.Map<ClientDTO>(client);

            return Ok(ApiResponse<ClientDTO>.Ok(updatedDto, HttpStatusCode.OK, traceId));
        }
    }
}
