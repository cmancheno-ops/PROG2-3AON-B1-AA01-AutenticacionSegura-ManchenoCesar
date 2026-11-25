using System.Security.Claims;
using AutoTaller.Business.Abstractions;
using AutoTaller.Business.Models;
using AutoTaller.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTaller.Web.Controllers;

/// <summary>Presentación: solo recibe/valida el formulario y delega las reglas en IAuthService.</summary>
public class AccountController : Controller
{
    private readonly IAuthService _auth;
    public AccountController(IAuthService auth) => _auth = auth;

    // ---------- Registro ----------
    [HttpGet, AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _auth.RegisterAsync(new RegisterRequest(model.NombreCompleto, model.Email, model.Password));
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e);
            return View(model);
        }
        TempData["Ok"] = "Cuenta creada. Ya puede iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    // ---------- Login / Logout ----------
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var status = await _auth.LoginAsync(model.Email, model.Password, model.RememberMe);
        if (status == LoginStatus.Success)
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction("Index", "Personal");

        // Mensaje genérico: no revela si el correo existe, si la contraseña falló o si la cuenta está bloqueada/deshabilitada.
        ModelState.AddModelError(string.Empty, "Credenciales inválidas o cuenta temporalmente no disponible. Intente de nuevo más tarde.");
        return View(model);
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        await _auth.LogoutAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
        return RedirectToAction("Index", "Home");
    }

    // ---------- Recuperación de contraseña ----------
    [HttpGet, AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        await _auth.RequestPasswordResetAsync(model.Email, (email, token) =>
            Url.Action(nameof(ResetPassword), "Account", new { email, token }, Request.Scheme)!);

        // Siempre la misma respuesta, exista o no la cuenta.
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet, AllowAnonymous]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return View("ResetPasswordInvalid");
        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _auth.ResetPasswordAsync(model.Email, model.Token, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e);
            return View(model);
        }
        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ResetPasswordConfirmation() => View();

    // ---------- Acceso denegado ----------
    [HttpGet, AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
