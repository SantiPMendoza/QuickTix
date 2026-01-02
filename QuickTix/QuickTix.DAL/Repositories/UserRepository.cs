using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using QuickTix.DAL.Data;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.DTOs;
using QuickTix.Contracts.DTOs.UserAuthDTO;
using QuickTix.Core.Models.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace QuickTix.DAL.Repositories
{
    /// <summary>
    /// Repositorio de usuarios: registro, login y gestión básica de roles.
    /// Implementación basada en ASP.NET Identity.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly string _secretKey;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        // Tiempo de vida del token JWT en días.
        private const int TokenExpirationDays = 7;

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

            // Validación sencilla para evitar claves demasiado cortas en producción.
            if (_secretKey.Length < 32)
            {
                throw new ArgumentException("ApiSettings:SecretKey debe tener al menos 32 caracteres.");
            }
        }

        // ============================================================
        // REGISTRO
        // ============================================================
        public async Task<UserLoginResponseDTO?> RegisterAsync(UserRegistrationDTO dto)
        {
            // Comprobamos si el nombre de usuario ya existe.
            var exists = await _context.Users.AnyAsync(u => u.UserName == dto.UserName);
            if (exists)
                return null;

            // Creamos la entidad de Identity que se va a persistir.
            var user = new AppUser
            {
                UserName = dto.UserName,
                Email = dto.UserName,
                Name = dto.Name
            };

            // Alta en Identity con contraseña.
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return null;

            // Nos aseguramos de que existan los roles básicos en la tabla AspNetRoles.
            string[] defaultRoles = { "admin", "manager", "client" };
            foreach (var role in defaultRoles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Por defecto asignamos el rol "client".
            await _userManager.AddToRoleAsync(user, "client");

            // Obtenemos el rol efectivo del usuario recién creado.
            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault() ?? string.Empty;

            // Construimos el DTO externo que se enviará al cliente.
            var userDto = new UserDTO
            {
                Id = user.Id,
                Name = user.Name,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = roleName,

                MustChangePassword = user.MustChangePassword
            };

            // En el registro no generamos token (se mantiene el comportamiento original).
            return new UserLoginResponseDTO
            {
                User = userDto,
                Token = string.Empty
            };
        }

        // ============================================================
        // LOGIN + JWT
        // ============================================================
        public async Task<UserLoginResponseDTO?> LoginAsync(UserLoginDTO dto)
        {
            // Localizamos el usuario a través de Identity.
            var user = await _userManager.FindByNameAsync(dto.UserName);
            if (user == null)
                return null;

            // Validamos la contraseña usando Identity (gestiona hashing y seguridad).
            bool valid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!valid)
                return null;

            // Roles actuales del usuario.
            var roles = await _userManager.GetRolesAsync(user);

            // Lista de claims que se incluirán dentro del JWT.
            // Estas claims son la "identidad" que luego leerán las APIs protegidas.
            var claims = new List<Claim>
{
    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
    new Claim(ClaimTypes.NameIdentifier, user.Id),
    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
};


            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            // Clave simétrica para firmar el token.
            var keyBytes = Encoding.UTF8.GetBytes(_secretKey);
            var signingKey = new SymmetricSecurityKey(keyBytes);

            // Credenciales de firma con algoritmo HMAC SHA256.
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

            // Creación y serialización del JWT.
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Proyectamos AppUser a UserDTO de forma explícita.
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



            // Devolvemos el DTO de usuario consumible por Desktop/Mobile más el token.
            return new UserLoginResponseDTO
            {
                Token = tokenString,
                User = userDto
            };
        }

        // ============================================================
        // LISTADO DE USUARIOS COMO DTO
        // ============================================================
        public async Task<List<UserDTO>> GetUserDTOsAsync()
        {
            // Obtenemos todos los usuarios de Identity.
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

                    MustChangePassword= u.MustChangePassword
                });
            }

            return dtos;
        }

        // ============================================================
        // USUARIO INDIVIDUAL COMO DTO
        // ============================================================
        public async Task<UserDTO?> GetUserAsync(string id)
        {
            // Localizamos el usuario en la tabla de Identity.
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return null;

            // Obtenemos sus roles actuales.
            var roles = await _userManager.GetRolesAsync(user);

            // Proyectamos a DTO para no exponer AppUser directamente.
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



        // ============================================================
        // UTILIDADES
        // ============================================================
        public async Task<bool> IsUniqueUserAsync(string userName)
        {
            return !await _context.Users.AnyAsync(u => u.UserName == userName);
        }
    }
}
