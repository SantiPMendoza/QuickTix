using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using QuickTix.DAL.Data;
using QuickTix.Core.Interfaces;
using QuickTix.Contracts.DTOs.UserAuthDTO;
using QuickTix.Core.Models.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace QuickTix.DAL.Repositories
{
    /// <summary>
    /// Repositorio de usuarios: registro, login y gestión básica de credenciales.
    /// Implementación basada en ASP.NET Identity.
    ///
    /// Este repositorio:
    /// - Crea usuarios y asigna roles por defecto en registro.
    /// - Valida credenciales en login y genera JWT con claims necesarias para la app.
    /// - Expone consultas simples de usuarios proyectadas a DTO.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly string _secretKey;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        /// <summary>
        /// Tiempo de vida del token JWT en días.
        /// </summary>
        private const int TokenExpirationDays = 7;

        /// <summary>
        /// Inicializa una nueva instancia del <see cref="UserRepository"/>.
        /// </summary>
        /// <param name="context">DbContext de la aplicación.</param>
        /// <param name="config">Configuración para obtener la clave JWT.</param>
        /// <param name="userManager">Gestor de usuarios Identity.</param>
        /// <param name="roleManager">Gestor de roles Identity.</param>
        /// <exception cref="ArgumentNullException">Si la clave de configuración no existe.</exception>
        /// <exception cref="ArgumentException">Si la clave es demasiado corta.</exception>
        public UserRepository(
            ApplicationDbContext context,
            IConfiguration config,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _secretKey = config.GetValue<string>("ApiSettings:SecretKey")
                         ?? throw new ArgumentNullException("ApiSettings:SecretKey no puede ser nulo.");
            _userManager = userManager;
            _roleManager = roleManager;

            // Validación para evitar claves demasiado cortas.
            if (_secretKey.Length < 32)
            {
                throw new ArgumentException("ApiSettings:SecretKey debe tener al menos 32 caracteres.");
            }
        }

        /// <summary>
        /// Registra un usuario en Identity y asigna por defecto el rol "client".
        /// No genera token en el registro (mantiene el comportamiento actual).
        /// </summary>
        /// <param name="dto">Datos de registro del usuario.</param>
        /// <returns>DTO de usuario registrado; null si el usuario ya existe o falla el alta.</returns>
        public async Task<UserLoginResponseDTO?> RegisterAsync(UserRegistrationDTO dto)
        {
            // Comprobación simple de existencia de username.
            var exists = await _context.Users.AnyAsync(u => u.UserName == dto.UserName);
            if (exists)
                return null;

            var user = new AppUser
            {
                UserName = dto.UserName,
                Email = dto.UserName,
                Name = dto.Name
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return null;

            // Asegura roles básicos.
            string[] defaultRoles = { "admin", "manager", "client" };
            foreach (var role in defaultRoles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Por defecto, rol client.
            await _userManager.AddToRoleAsync(user, "client");

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault() ?? string.Empty;

            var userDto = new UserDTO
            {
                Id = user.Id,
                Name = user.Name,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = roleName,
                MustChangePassword = user.MustChangePassword
            };

            return new UserLoginResponseDTO
            {
                User = userDto,
                Token = string.Empty
            };
        }

        /// <summary>
        /// Valida credenciales con Identity y genera un JWT con roles y claims adicionales
        /// según el tipo de usuario (manager/client).
        /// </summary>
        /// <param name="dto">Credenciales de login.</param>
        /// <returns>DTO de usuario y token; null si falla autenticación.</returns>
        public async Task<UserLoginResponseDTO?> LoginAsync(UserLoginDTO dto)
        {
            var user = await _userManager.FindByNameAsync(dto.UserName);
            if (user == null)
                return null;

            bool valid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!valid)
                return null;

            var roles = await _userManager.GetRolesAsync(user);

            // Claims base incluidas en el JWT.
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),

                new Claim("name", user.Name ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),

                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            // Ajuste mínimo por seguridad funcional:
            // En el sistema los roles se crean como "manager" (minúsculas),
            // pero aquí se validaba "Manager", lo que impedía añadir claims.
            if (roles.Any(r => r.Equals("manager", StringComparison.OrdinalIgnoreCase)))
            {
                var manager = await _context.Managers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.AppUserId == user.Id);

                if (manager == null)
                    throw new InvalidOperationException("El usuario tiene rol manager pero no existe registro Manager asociado.");

                claims.Add(new Claim("managerId", manager.Id.ToString()));
                claims.Add(new Claim("venueId", manager.VenueId.ToString()));
            }

            if (roles.Any(r => r.Equals("client", StringComparison.OrdinalIgnoreCase)))
            {
                var client = await _context.Clients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.AppUserId == user.Id);

                if (client == null)
                    throw new InvalidOperationException("El usuario tiene rol client pero no existe registro Client asociado.");

                claims.Add(new Claim("clientId", client.Id.ToString()));
            }

            var keyBytes = Encoding.UTF8.GetBytes(_secretKey);
            var signingKey = new SymmetricSecurityKey(keyBytes);

            var signingCredentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(TokenExpirationDays),
                Issuer = "QuickTix.API",
                Audience = "QuickTix.Clients",
                SigningCredentials = signingCredentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var roleName = roles.FirstOrDefault() ?? string.Empty;

            var userDto = new UserDTO
            {
                Id = user.Id,
                Name = user.Name,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = roleName,
                MustChangePassword = user.MustChangePassword
            };

            return new UserLoginResponseDTO
            {
                Token = tokenString,
                User = userDto
            };
        }

        /// <summary>
        /// Obtiene el listado de usuarios proyectado a <see cref="UserDTO"/>.
        /// Incluye la lectura del rol actual por usuario.
        /// </summary>
        /// <returns>Listado de usuarios como DTO.</returns>
        public async Task<List<UserDTO>> GetUserDTOsAsync()
        {
            var users = await _context.Users.ToListAsync();
            var dtos = new List<UserDTO>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);

                dtos.Add(new UserDTO
                {
                    Id = u.Id,
                    Name = u.Name,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? string.Empty,
                    MustChangePassword = u.MustChangePassword
                });
            }

            return dtos;
        }

        /// <summary>
        /// Obtiene un usuario por id proyectado a <see cref="UserDTO"/>.
        /// </summary>
        /// <param name="id">Identificador Identity del usuario.</param>
        /// <returns>Usuario como DTO si existe; en caso contrario, null.</returns>
        public async Task<UserDTO?> GetUserAsync(string id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDTO
            {
                Id = user.Id,
                Name = user.Name,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty,
                MustChangePassword = user.MustChangePassword
            };
        }

        /// <summary>
        /// Cambia la contraseña del usuario validando la contraseña actual.
        /// Si el usuario tenía <see cref="AppUser.MustChangePassword"/> en true, lo desactiva tras el cambio.
        /// </summary>
        /// <param name="userId">Identificador Identity del usuario.</param>
        /// <param name="currentPassword">Contraseña actual.</param>
        /// <param name="newPassword">Nueva contraseña.</param>
        /// <exception cref="KeyNotFoundException">Si el usuario no existe.</exception>
        /// <exception cref="InvalidOperationException">Si Identity rechaza el cambio o la actualización.</exception>
        public async Task ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("Usuario no encontrado.");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (!result.Succeeded)
            {
                var msg = string.Join(" ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException(msg);
            }

            if (user.MustChangePassword)
            {
                user.MustChangePassword = false;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var msg = string.Join(" ", updateResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException(msg);
                }
            }
        }

        /// <summary>
        /// Comprueba si un nombre de usuario es único.
        /// </summary>
        /// <param name="userName">Nombre de usuario (Identity UserName).</param>
        /// <returns>True si no existe; en caso contrario, false.</returns>
        public async Task<bool> IsUniqueUserAsync(string userName)
        {
            return !await _context.Users.AnyAsync(u => u.UserName == userName);
        }
    }
}
