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
    /// Controlador API para la gestión de clientes (abonados) y su vinculación con Identity.
    /// Centraliza la creación/actualización del AppUser asociado y la asignación del rol "client".
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ClientController : BaseController<Client, ClientDTO, CreateClientDTO>
    {
        // Gestor de usuarios Identity para crear/actualizar el AppUser asociado al cliente
        private readonly UserManager<AppUser> _userManager;

        // Gestor de roles Identity para asegurar y asignar el rol "client"
        private readonly RoleManager<IdentityRole> _roleManager;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="ClientController"/>.
        /// </summary>
        /// <param name="repository">Repositorio de clientes.</param>
        /// <param name="mapper">Servicio de mapeo entre entidades y DTOs.</param>
        /// <param name="logger">Logger del controlador.</param>
        /// <param name="userManager">Gestor de usuarios Identity.</param>
        /// <param name="roleManager">Gestor de roles Identity.</param>
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

        /// <summary>
        /// Crea un cliente y su AppUser asociado en Identity, asignándole el rol "client".
        /// Valida duplicados por NIF/NIE y teléfono antes de persistir.
        /// </summary>
        /// <param name="dto">Datos de creación del cliente.</param>
        /// <returns>Cliente creado.</returns>
        [HttpPost]
        [Authorize(Roles = "admin")]
        public override async Task<IActionResult> Create([FromBody] CreateClientDTO dto)
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

            var createdResponse = BuildOk(responseDto, HttpStatusCode.Created);

            return CreatedAtAction(nameof(Get), new { id = responseDto.Id }, createdResponse);
        }

        /// <summary>
        /// Actualiza un cliente y su AppUser asociado en Identity.
        /// Valida duplicados por NIF/NIE y teléfono excluyendo el usuario actual.
        /// </summary>
        /// <param name="id">Identificador del cliente.</param>
        /// <param name="dto">Datos actualizados del cliente.</param>
        /// <returns>Cliente actualizado.</returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "admin")]
        public override async Task<IActionResult> Update(int id, [FromBody] ClientDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(BuildFail(
                    HttpStatusCode.BadRequest,
                    ExtractModelStateErrors(ModelState)
                ));
            }

            var client = await _repository.GetForUpdateAsync(id);
            if (client == null)
            {
                return NotFound(BuildFail(
                    HttpStatusCode.NotFound,
                    new[] { "Registro no encontrado." }
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

            return Ok(BuildOk(updatedDto, HttpStatusCode.OK));
        }
    }
}
