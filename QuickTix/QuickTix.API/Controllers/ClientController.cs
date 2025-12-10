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
    //[Authorize]
    [AllowAnonymous]
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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1) Crear AppUser asociado al cliente
            var appUser = new AppUser
            {
                UserName = dto.Email,  
                Email = dto.Email,
                Name = dto.Name,
                Nif = dto.Nif,
                PhoneNumber = dto.PhoneNumber,
                MustChangePassword = true
            };

            // Contraseña por defecto basada en el NIF (igual que en Admin/Manager)
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
            var response = _mapper.Map<ClientDTO>(client);

            return CreatedAtRoute(
                $"{ControllerContext.ActionDescriptor.ControllerName}_GetEntity",
                new { id = response.Id },
                response);
        }
    }
}
