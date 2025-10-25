using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.DTOs.UserAuthDTO;
using QuickTix.Core.Models.Entities;
using QuickTix.API.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;


namespace QuickTix.Data.Repositories
{
    /// <summary>
    /// Repositorio de usuarios: registro, login y gestión básica de roles.
    /// Implementación basada en ASP.NET Identity.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly QuickTixDbContext _context;
        private readonly string _secretKey;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private const int TokenExpirationDays = 7;

        public UserRepository(
            QuickTixDbContext context,
            IConfiguration config,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _secretKey = config.GetValue<string>("ApiSettings:SecretKey")
                ?? throw new InvalidOperationException("Falta ApiSettings:SecretKey en configuración.");

            if (_secretKey.Length < 32)
                throw new ArgumentException("La clave secreta debe tener al menos 32 caracteres.");
        }

        // ============================================================
        // REGISTRO
        // ============================================================
        public async Task<UserLoginResponseDTO?> Register(UserRegistrationDTO dto)
        {
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

            // Crear roles iniciales si no existen
            string[] defaultRoles = { "admin", "manager", "client" };
            foreach (var role in defaultRoles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    await _roleManager.CreateAsync(new IdentityRole(role));
            }

            // Por defecto asignamos el rol "client"
            await _userManager.AddToRoleAsync(user, "client");

            return new UserLoginResponseDTO
            {
                User = user,
                Token = string.Empty
            };
        }

        // ============================================================
        // LOGIN + JWT
        // ============================================================
        public async Task<UserLoginResponseDTO?> Login(UserLoginDTO dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName.ToLower() == dto.UserName.ToLower());

            if (user == null)
                return null;

            bool valid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!valid)
                return null;

            var roles = await _userManager.GetRolesAsync(user);
            var key = Encoding.ASCII.GetBytes(_secretKey);
            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault() ?? "client")
                }),
                Expires = DateTime.UtcNow.AddDays(TokenExpirationDays),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new UserLoginResponseDTO
            {
                Token = tokenHandler.WriteToken(token),
                User = user
            };
        }

        // ============================================================
        // USUARIOS
        // ============================================================
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
                    UserName = u.UserName,
                    Email = u.Email,
                    Role = roles.FirstOrDefault() ?? ""
                });
            }

            return dtos;
        }

        public async Task<AppUser?> GetUser(string id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<bool> IsUniqueUser(string userName)
        {
            return !await _context.Users.AnyAsync(u => u.UserName == userName);
        }
    }
}
