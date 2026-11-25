using AutoTaller.Business.Abstractions;
using AutoTaller.Business.Models;
using AutoTaller.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AutoTaller.Business.Services;

public class UserAdminService : IUserAdminService
{
    private readonly UserManager<ApplicationUser> _users;

    public UserAdminService(UserManager<ApplicationUser> users) => _users = users;

    public async Task<IReadOnlyList<UserListItem>> ListUsersAsync()
    {
        var all = await _users.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync();
        var list = new List<UserListItem>();
        foreach (var u in all)
        {
            var roles = await _users.GetRolesAsync(u);
            var locked = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow;
            list.Add(new UserListItem(u.Id, u.NombreCompleto, u.Email ?? "", string.Join(", ", roles), u.IsActive, locked));
        }
        return list;
    }

    public async Task<OperationResult> SetRoleAsync(string actingUserId, string targetUserId, string role)
    {
        if (!AppRoles.All.Contains(role)) return OperationResult.Fail("Rol no válido.");

        var denied = await EnsureActorIsAdminAsync(actingUserId);
        if (denied is not null) return denied;

        var target = await _users.FindByIdAsync(targetUserId);
        if (target is null) return OperationResult.Fail("Usuario no encontrado.");

        var currentRoles = await _users.GetRolesAsync(target);
        if (currentRoles.Count == 1 && currentRoles[0] == role) return OperationResult.Ok();

        var targetIsAdmin = currentRoles.Contains(AppRoles.Administrador);
        if (targetIsAdmin && role != AppRoles.Administrador && target.IsActive && await ActiveAdminCountAsync() <= 1)
            return OperationResult.Fail("No se puede quitar el rol a la última cuenta administradora activa.");

        var add = await _users.AddToRoleAsync(target, role);
        if (!add.Succeeded && !add.Errors.Any(e => e.Code == "UserAlreadyInRole"))
            return OperationResult.Fail(add.Errors.Select(e => e.Description));

        var toRemove = currentRoles.Where(r => r != role).ToList();
        if (toRemove.Count > 0)
        {
            var rem = await _users.RemoveFromRolesAsync(target, toRemove);
            if (!rem.Succeeded) return OperationResult.Fail(rem.Errors.Select(e => e.Description));
        }

        // Forzar a que las sesiones abiertas del usuario se revaliden con el rol nuevo.
        await _users.UpdateSecurityStampAsync(target);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> SetActiveAsync(string actingUserId, string targetUserId, bool isActive)
    {
        var denied = await EnsureActorIsAdminAsync(actingUserId);
        if (denied is not null) return denied;

        var target = await _users.FindByIdAsync(targetUserId);
        if (target is null) return OperationResult.Fail("Usuario no encontrado.");
        if (target.IsActive == isActive) return OperationResult.Ok();

        if (!isActive && await _users.IsInRoleAsync(target, AppRoles.Administrador) && await ActiveAdminCountAsync() <= 1)
            return OperationResult.Fail("No se puede deshabilitar la última cuenta administradora activa.");

        target.IsActive = isActive;
        var upd = await _users.UpdateAsync(target);
        if (!upd.Succeeded) return OperationResult.Fail(upd.Errors.Select(e => e.Description));

        if (!isActive) await _users.UpdateSecurityStampAsync(target);   // cierra sus sesiones activas
        return OperationResult.Ok();
    }

    // Defensa en profundidad: aunque el controlador ya tiene [Authorize(Roles=...)], el servicio vuelve a comprobar.
    private async Task<OperationResult?> EnsureActorIsAdminAsync(string actingUserId)
    {
        var actor = await _users.FindByIdAsync(actingUserId);
        if (actor is null || !actor.IsActive || !await _users.IsInRoleAsync(actor, AppRoles.Administrador))
            return OperationResult.Fail("No tiene permisos para realizar esta operación.");
        return null;
    }

    private async Task<int> ActiveAdminCountAsync()
        => (await _users.GetUsersInRoleAsync(AppRoles.Administrador)).Count(u => u.IsActive);
}
