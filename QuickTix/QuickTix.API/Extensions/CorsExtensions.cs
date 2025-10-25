using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace QuickTix.API.Extensions
{
    /// <summary>
    /// Configuración CORS para permitir orígenes según entorno.
    /// </summary>
    public static class CorsExtensions
    {
        public static IServiceCollection AddQuickTixCors(this IServiceCollection services, IWebHostEnvironment env)
        {
            var allowedOrigins = env.IsDevelopment()
                ? new[] { "http://localhost:4200", "http://localhost:5000" }
                : new[] { "https://quicktix.es" };

            services.AddCors(options =>
            {
                options.AddPolicy("QuickTixCors", builder =>
                {
                    builder.WithOrigins(allowedOrigins)
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });

            return services;
        }
    }
}
