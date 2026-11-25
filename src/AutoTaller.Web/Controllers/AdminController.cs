using System.Security.Claims;
using AutoTaller.Business;
using AutoTaller.Business.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTaller.Web.Controllers;

/// <summary>Autorización en servidor: todo el controlador exige el rol Administrador.</summary>
[Authorize(Roles = AppRoles.Administrador)]
public class AdminController : Controller
{
    private readonly IUserAdminService _admin;
    public AdminController(IUserAdminService admin) => _admin = admin;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Users() => View(await _admin.ListUsersAsync());

    [HttpPost]
    public async Task<IActionResult> SetRole(string id, string role)
    {
        var r = await _admin.SetRoleAsync(CurrentUserId, id, role);
        TempData[r.Succeeded ? "Ok" : "Error"] = r.Succeeded ? "Rol actualizado." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(string id, bool isActive)
    {
        var r = await _admin.SetActiveAsync(CurrentUserId, id, isActive);
        TempData[r.Succeeded ? "Ok" : "Error"] = r.Succeeded ? "Estado actualizado." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Users));
    }
}
