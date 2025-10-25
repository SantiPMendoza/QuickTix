using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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

            // JWT propio
            services.AddAuthentication()
                .AddJwtBearer("JwtOwn", options =>
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
                        ValidateLifetime = true
                    };
                })
                // JWT de Google
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

            // Política combinada
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes("JwtOwn", "JwtGoogle")
                    .RequireAuthenticatedUser()
                    .Build();
            });

            return services;
        }
    }
}
