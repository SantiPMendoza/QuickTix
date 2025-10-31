using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuickTix.DAL.Data;
using QuickTix.API.AutoMapper;
using QuickTix.API.Extensions;
using QuickTix.Core.Interfaces;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ===================================================
// 🔹 CONFIGURAR DB CONTEXT
// ===================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlConnection")));

// ===================================================
// 🔹 CONFIGURAR IDENTITY
// ===================================================
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ===================================================
// 🔹 REGISTRAR SERVICIOS Y REPOSITORIOS
// ===================================================
builder.Services.AddScoped<IUserRepository, UserRepository>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// ===================================================
// 🔹 APLICAR EXTENSIONES
// ===================================================
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddQuickTixCors(builder.Environment);
builder.Services.AddSwaggerWithAuth();

// ===================================================
// 🔹 CONTROLADORES
// ===================================================
builder.Services.AddControllers();

// ===================================================
// 🔹 CONSTRUIR APLICACIÓN
// ===================================================
var app = builder.Build();

// ===================================================
// 🔹 PIPELINE HTTP
// ===================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("QuickTixCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
