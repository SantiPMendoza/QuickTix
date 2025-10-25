using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuickTix.API.Data;
using QuickTix.API.Mapping;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.Data.Repositories;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================
// CONFIGURAR DB CONTEXT
// ============================
builder.Services.AddDbContext<QuickTixDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlConnection")));

// ============================
// CONFIGURAR IDENTITY
// ============================
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<QuickTixDbContext>()
    .AddDefaultTokenProviders();

// ============================
// CONFIGURAR JWT
// ============================
var secretKey = builder.Configuration.GetValue<string>("ApiSettings:SecretKey");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
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

// ============================
// INYECCIÓN DE DEPENDENCIAS
// ============================
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddAutoMapper(typeof(MappingProfile));


// ============================
// SWAGGER + CONTROLLERS
// ============================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
