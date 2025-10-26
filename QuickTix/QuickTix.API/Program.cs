using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuickTix.API.Data;
using QuickTix.API.Mapping;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.Data.Repositories;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===================================================
// 🔹 CONFIGURAR DB CONTEXT
// ===================================================
builder.Services.AddDbContext<QuickTixDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlConnection")));

// ===================================================
// 🔹 CONFIGURAR IDENTITY
// ===================================================
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<QuickTixDbContext>()
    .AddDefaultTokenProviders();

// ===================================================
// 🔹 REGISTRAR SERVICIOS Y REPOSITORIOS
// ===================================================
builder.Services.AddScoped<IUserRepository, UserRepository>();

// AutoMapper (mapeos de entidades ↔ DTO)
builder.Services.AddAutoMapper(typeof(MappingProfile));

// ===================================================
// 🔹 CONFIGURAR AUTENTICACIÓN JWT
// ===================================================
var secretKey = builder.Configuration.GetValue<string>("ApiSettings:SecretKey");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer("JwtOwn", options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// Política de autorización por defecto
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes("JwtOwn")
        .RequireAuthenticatedUser()
        .Build();
});

// ===================================================
// 🔹 CONFIGURAR CORS (Frontend: WPF y MAUI)
// ===================================================
var allowedOrigins = builder.Environment.IsDevelopment()
    ? new[] { "http://localhost:5000", "http://localhost:4200" } // para desarrollo
    : new[] { "https://quicktix.app" }; // dominio real futuro

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ===================================================
// 🔹 CONFIGURAR SWAGGER
// ===================================================
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Auth Bearer Token.\r\nEjemplo: Bearer {tu_token_jwt}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

// ===================================================
// 🔹 CONTROLADORES + ENDPOINTS
// ===================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ===================================================
// 🔹 CONSTRUIR APLICACIÓN
// ===================================================
var app = builder.Build();

// ===================================================
// 🔹 SEEDING DE DATOS (opcional, futuro)
// ===================================================
// using (var scope = app.Services.CreateScope())
// {
//     var services = scope.ServiceProvider;
//     await AppDbSeeder.SeedAsync(services);
// }

// ===================================================
// 🔹 PIPELINE HTTP
// ===================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
