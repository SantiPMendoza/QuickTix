using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace QuickTix.API.Extensions
{
    /// <summary>
    /// Extensión para configurar autenticación JWT (propio + Google).
    /// </summary>
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var key = configuration["ApiSettings:SecretKey"];
            var googleClientId = configuration["Google:ClientId"];

            services.AddAuthentication(options =>
            {
                // 🔹 Usa siempre "Bearer" como esquema principal
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            // 🔹 Token propio (para login con Identity)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key!)),
                    ValidateIssuer = true,
                    ValidIssuer = "QuickTix.API",
                    ValidateAudience = true,
                    ValidAudience = "QuickTix.Clients",
                    ValidateLifetime = true,
                    RoleClaimType = ClaimTypes.Role // ✅ importantísimo
                };
            })
            // 🔹 Token de Google (para logins federados)
            .AddJwtBearer("JwtGoogle", options =>
            {
                options.Authority = "https://accounts.google.com";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "https://accounts.google.com",
                    ValidateAudience = true,
                    ValidAudience = googleClientId,
                    ValidateLifetime = true
                };
            });

            // 🔹 Política estándar (ya usa el esquema "Bearer")
            services.AddAuthorization();

            return services;
        }
    }

}
