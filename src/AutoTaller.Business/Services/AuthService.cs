using System.Text;
using System.Text.Encodings.Web;
using AutoTaller.Business.Abstractions;
using AutoTaller.Business.Models;
using AutoTaller.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace AutoTaller.Business.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IAppEmailSender _email;
    private readonly ILogger<AuthService> _log;

    public AuthService(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn,
        IAppEmailSender email, ILogger<AuthService> log)
    {
        _users = users; _signIn = signIn; _email = email; _log = log;
    }

    public async Task<OperationResult> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            NombreCompleto = request.NombreCompleto.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // CreateAsync aplica la política de contraseña, valida correo único y guarda el HASH (PasswordHasher: PBKDF2 + sal).
        var created = await _users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return OperationResult.Fail(created.Errors.Select(e => e.Description).Distinct());

        // El registro público SIEMPRE asigna Cliente; el rol nunca viene del formulario.
        var roleResult = await _users.AddToRoleAsync(user, AppRoles.Cliente);
        if (!roleResult.Succeeded)
        {
            await _users.DeleteAsync(user);
            _log.LogError("No se pudo asignar el rol Cliente al registrar un usuario.");
            return OperationResult.Fail("No se pudo completar el registro. Intente más tarde.");
        }
        return OperationResult.Ok();
    }

    public async Task<LoginStatus> LoginAsync(string email, string password, bool rememberMe)
    {
        var user = await _users.FindByEmailAsync(email.Trim());
        if (user is null || !user.IsActive)
            return LoginStatus.Failed;   // mismo resultado que credenciales inválidas (mensaje genérico)

        var result = await _signIn.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);
        if (result.Succeeded) return LoginStatus.Success;
        if (result.IsLockedOut) return LoginStatus.LockedOut;
        return LoginStatus.Failed;
    }

    public async Task LogoutAsync(string? userId)
    {
        await _signIn.SignOutAsync();
        if (userId is null) return;
        var user = await _users.FindByIdAsync(userId);
        // Rotar el SecurityStamp invalida cualquier copia de la cookie (cierra la sesión en el servidor).
        if (user is not null) await _users.UpdateSecurityStampAsync(user);
    }

    public async Task RequestPasswordResetAsync(string email, Func<string, string, string> buildResetUrl)
    {
        var user = await _users.FindByEmailAsync(email.Trim());
        // Respuesta idéntica exista o no el correo: no se revela qué cuentas existen.
        if (user is null || !user.IsActive) return;

        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = buildResetUrl(user.Email!, encoded);

        var body = $"<p>Hola {HtmlEncoder.Default.Encode(user.NombreCompleto)},</p>" +
                   "<p>Recibimos una solicitud para restablecer su contraseña. El enlace caduca en poco tiempo y solo puede usarse una vez:</p>" +
                   $"<p><a href=\"{HtmlEncoder.Default.Encode(link)}\">Restablecer contraseña</a></p>" +
                   "<p>Si usted no lo solicitó, ignore este mensaje.</p>";
        await _email.SendAsync(user.Email!, "Restablecer contraseña - AutoTaller", body);
    }

    public async Task<OperationResult> ResetPasswordAsync(string email, string encodedToken, string newPassword)
    {
        const string invalid = "El enlace no es válido o ha caducado.";
        var user = await _users.FindByEmailAsync(email.Trim());
        if (user is null || !user.IsActive) return OperationResult.Fail(invalid);

        string token;
        try { token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken)); }
        catch (FormatException) { return OperationResult.Fail(invalid); }

        // ResetPasswordAsync valida firma + caducidad + SecurityStamp y luego rota el stamp => token de un solo uso.
        var result = await _users.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).Distinct().ToList();
            return OperationResult.Fail(errors);
        }

        await _users.ResetAccessFailedCountAsync(user);
        await _users.SetLockoutEndDateAsync(user, null);
        return OperationResult.Ok();
    }

    public async Task<ProfileDto?> GetProfileAsync(string userId)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return null;
        var roles = await _users.GetRolesAsync(user);
        return new ProfileDto(user.Id, user.NombreCompleto, user.Email ?? "", string.Join(", ", roles), user.CreatedAtUtc);
    }

    public async Task<OperationResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return OperationResult.Fail("Usuario no encontrado.");

        var result = await _users.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            return OperationResult.Fail(result.Errors.Select(e => e.Description).Distinct());

        await _signIn.RefreshSignInAsync(user);   // mantiene la sesión actual con el stamp nuevo
        return OperationResult.Ok();
    }
}
