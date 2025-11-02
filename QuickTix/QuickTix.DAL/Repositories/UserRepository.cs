using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using QuickTix.DAL.Data;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.DTOs.UserAuthDTO;
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
        private const int TokenExpirationDays = 7;

        public UserRepository(ApplicationDbContext context, IConfiguration config,
            UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _secretKey = config.GetValue<string>("ApiSettings:SecretKey");
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ============================================================
        // REGISTRO
        // ============================================================
        public async Task<UserLoginResponseDTO?> RegisterAsync(UserRegistrationDTO dto)
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
        public async Task<UserLoginResponseDTO?> LoginAsync(UserLoginDTO dto)
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

        public async Task<AppUser?> GetUserAsync(string id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }
        
        public async Task<bool> IsUniqueUserAsync(string userName)
        {
            return !await _context.Users.AnyAsync(u => u.UserName == userName);
        }
    }
}
