using AutoTaller.Business.Models;
using AutoTaller.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoTaller.Business.Services;

/// <summary>Inicialización controlada: crea roles y el administrador inicial desde configuración (nunca desde el registro público).</summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");
        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var opt = sp.GetRequiredService<IOptions<SeedOptions>>().Value;

        foreach (var role in AppRoles.All)
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));

        if (string.IsNullOrWhiteSpace(opt.AdminEmail) || string.IsNullOrWhiteSpace(opt.AdminPassword))
        {
            log.LogWarning("Seed:AdminEmail / Seed:AdminPassword no configurados: no se creó administrador inicial.");
        }
        else
        {
            await EnsureUserAsync(users, log, opt.AdminEmail, opt.AdminPassword, opt.AdminName, AppRoles.Administrador);
        }

        if (!string.IsNullOrWhiteSpace(opt.DemoClientEmail) && !string.IsNullOrWhiteSpace(opt.DemoClientPassword))
            await EnsureUserAsync(users, log, opt.DemoClientEmail, opt.DemoClientPassword, "Cliente de Prueba", AppRoles.Cliente);
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> users, ILogger log,
        string email, string password, string name, string role)
    {
        if (await users.FindByEmailAsync(email) is not null) return;

        var user = new ApplicationUser
        {
            UserName = email, Email = email, EmailConfirmed = true,
            NombreCompleto = name, IsActive = true, CreatedAtUtc = DateTime.UtcNow
        };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            log.LogError("No se pudo crear {Email}: {Errors}", email, string.Join("; ", created.Errors.Select(e => e.Code)));
            return;
        }
        await users.AddToRoleAsync(user, role);
        log.LogInformation("Usuario inicial {Email} creado con rol {Role}.", email, role);
    }
}
