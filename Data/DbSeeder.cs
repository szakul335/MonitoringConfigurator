using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace MonitoringConfigurator.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;

            var ctx = sp.GetRequiredService<AppDbContext>();

            // Aplikowanie migracji lub utworzenie bazy, jeśli nie istnieje
            try
            {
                await ctx.Database.MigrateAsync();
            }
            catch
            {
                await ctx.Database.EnsureCreatedAsync();
            }

            // Seedowanie użytkownika Admin (tylko jeśli nie istnieje)
            var um = sp.GetRequiredService<UserManager<IdentityUser>>();
            var adminEmail = "admin@demo.pl";
            var adminPass = "Admin123!";

            if (await um.FindByEmailAsync(adminEmail) is null)
            {
                var user = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                await um.CreateAsync(user, adminPass);
            }

            // Sekcja seedowania produktów została usunięta.
        }
    }
}