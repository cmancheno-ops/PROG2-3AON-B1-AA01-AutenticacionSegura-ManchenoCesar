using System.Security.Claims;
using AutoTaller.Business.Abstractions;
using AutoTaller.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTaller.Web.Controllers;

/// <summary>Perfil: el id SIEMPRE sale del claim de la sesión, nunca de la URL ni del formulario (solo datos propios).</summary>
[Authorize]
public class ProfileController : Controller
{
    private readonly IAuthService _auth;
    public ProfileController(IAuthService auth) => _auth = auth;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var profile = await _auth.GetProfileAsync(CurrentUserId);
        return profile is null ? Forbid() : View(profile);
    }

    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _auth.ChangePasswordAsync(CurrentUserId, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e);
            return View(model);
        }
        TempData["Ok"] = "Contraseña actualizada correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
