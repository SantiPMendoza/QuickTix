using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuickTix.Core.Enums;
using QuickTix.Core.Models.Entities;
using QuickTix.DAL.Data;

namespace QuickTix.DAL.Data
{
    public static class AppDbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            // ---- 1️⃣ Crear rol administrador ----
            const string adminRole = "admin";
            if (!await roleManager.RoleExistsAsync(adminRole))
                await roleManager.CreateAsync(new IdentityRole(adminRole));

            // ---- 2️⃣ Crear usuario administrador ----
            const string adminEmail = "admin2@quicktix.com";
            const string adminPassword = "Abcd123!";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new AppUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    Name = "Administrador"
                };
                await userManager.CreateAsync(adminUser, adminPassword);
                await userManager.AddToRoleAsync(adminUser, adminRole);
            }

            // ---- 3️⃣ Registrar entidad Admin ----
            if (!await context.Admins.AnyAsync())
            {
                context.Admins.Add(new Admin
                {
                    Name = "Administrador",
                    AppUserId = adminUser.Id
                });
                await context.SaveChangesAsync();
            }

            // ---- 4️⃣ Crear Venues ----
            if (!await context.Venues.AnyAsync())
            {
                context.Venues.AddRange(
                    new Venue
                    {
                        Name = "Piscina Municipal de Nalda",
                        Location = "Calle Mayor 15, Nalda",
                        Capacity = 150,
                        IsActive = true
                    },
                    new Venue
                    {
                        Name = "Frontón Municipal",
                        Location = "Av. La Rioja 8, Nalda",
                        Capacity = 80,
                        IsActive = true
                    }
                );
                await context.SaveChangesAsync();
            }

            // ---- 5️⃣ Crear Manager ----
            if (!await context.Managers.AnyAsync())
            {
                var managerEmail = "manager@quicktix.com";
                var managerUser = await userManager.FindByEmailAsync(managerEmail);
                if (managerUser == null)
                {
                    managerUser = new AppUser
                    {
                        UserName = managerEmail,
                        Email = managerEmail,
                        Name = "Gestor Piscina"
                    };
                    await userManager.CreateAsync(managerUser, "Abcd123!");
                }

                var firstVenue = await context.Venues.FirstAsync();
                context.Managers.Add(new Manager
                {
                    Name = "Gestor Piscina",
                    AppUserId = managerUser.Id,
                    VenueId = firstVenue.Id
                });

                await context.SaveChangesAsync();
            }

            // ---- 6️⃣ Crear Clientes ----
            if (!await context.Clients.AnyAsync())
            {
                for (int i = 1; i <= 3; i++)
                {
                    var email = $"cliente{i}@quicktix.com";
                    var clientUser = await userManager.FindByEmailAsync(email);
                    if (clientUser == null)
                    {
                        clientUser = new AppUser
                        {
                            UserName = email,
                            Email = email,
                            Name = $"Cliente {i}"
                        };
                        await userManager.CreateAsync(clientUser, "Abcd123!");
                    }

                    context.Clients.Add(new Client
                    {
                        Name = $"Cliente {i}",
                        AppUserId = clientUser.Id
                    });
                }
                await context.SaveChangesAsync();
            }

            // ---- 7️⃣ Crear Tickets de prueba ----
            if (!await context.Tickets.AnyAsync())
            {
                var venue = await context.Venues.FirstAsync();
                var client = await context.Clients.FirstAsync();

                context.Tickets.AddRange(
                    new Ticket
                    {
                        VenueId = venue.Id,
                        ClientId = client.Id,
                        Type = TicketType.AdultoLaboral,
                        Context = TicketContext.Normal,
                        Price = 4.5m,
                        PurchaseDate = DateTime.UtcNow
                    },
                    new Ticket
                    {
                        VenueId = venue.Id,
                        ClientId = client.Id,
                        Type = TicketType.AdultoFestivo,
                        Context = TicketContext.InvitadoAbonado,
                        Price = 3.5m,
                        PurchaseDate = DateTime.UtcNow.AddDays(-1)
                    }
                );
                await context.SaveChangesAsync();
            }

            // ---- 8️⃣ Crear Subscriptions ----
            if (!await context.Subscriptions.AnyAsync())
            {
                var venue = await context.Venues.FirstAsync();
                var client = await context.Clients.FirstAsync();

                context.Subscriptions.Add(new Subscription
                {
                    VenueId = venue.Id,
                    ClientId = client.Id,
                    Category = SubscriptionCategory.Adulto,
                    Duration = SubscriptionDuration.Mensual,
                    Price = 25m,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1)
                });

                await context.SaveChangesAsync();
            }

            // ---- 9️⃣ Crear Sales ----
            if (!await context.Sales.AnyAsync())
            {
                var venue = await context.Venues.FirstAsync();
                var manager = await context.Managers.FirstAsync();
                var ticket = await context.Tickets.FirstAsync();
                var subscription = await context.Subscriptions.FirstAsync();

                context.Sales.AddRange(
                    new Sale
                    {
                        VenueId = venue.Id,
                        ManagerId = manager.Id,
                        TicketId = ticket.Id,
                        Amount = ticket.Price,
                        Date = DateTime.UtcNow
                    },
                    new Sale
                    {
                        VenueId = venue.Id,
                        ManagerId = manager.Id,
                        SubscriptionId = subscription.Id,
                        Amount = subscription.Price,
                        Date = DateTime.UtcNow.AddMinutes(-10)
                    }
                );

                await context.SaveChangesAsync();
            }
        }
    }
}
