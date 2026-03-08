using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using LeveLEO.Features.Identity.Models;

namespace LeveLEO.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IHost host)
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<AppDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Видаляємо і створюємо БД заново (тільки в Development)
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            // Створюємо ролі
            string[] roles = { "Admin", "Moderator", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Дефолтний адмін
            var adminEmail = "admin@leveleo.com";
            var adminPassword = "Admin123!@#";  // Зміни на свій

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "LeveLEO",
                    Language = "uk",
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine($"Дефолтний адмін створено: {adminEmail} / {adminPassword}");
                }
                else
                {
                    Console.WriteLine("Помилка створення адміна: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                Console.WriteLine($"Адмін вже існує: {adminEmail}");
            }
        }
    }
}