
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

// Registrar IMemoryCache
builder.Services.AddMemoryCache();

// ===================================================
// 🔹 REGISTRAR SERVICIOS Y REPOSITORIOS
// ===================================================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IManagerRepository, ManagerRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<IVenueRepository, VenueRepository>();

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



// Seeding inicial de datos
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await AppDbSeeder.SeedAsync(services);
}


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
