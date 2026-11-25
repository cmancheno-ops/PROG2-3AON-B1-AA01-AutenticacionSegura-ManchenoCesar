using AutoTaller.Business.Models;

namespace AutoTaller.Business.Abstractions;

public interface IAuthService
{
    Task<OperationResult> RegisterAsync(RegisterRequest request);
    Task<LoginStatus> LoginAsync(string email, string password, bool rememberMe);
    Task LogoutAsync(string? userId);

    /// <param name="buildResetUrl">(email, tokenCodificado) → URL absoluta; la construye la capa Web.</param>
    Task RequestPasswordResetAsync(string email, Func<string, string, string> buildResetUrl);
    Task<OperationResult> ResetPasswordAsync(string email, string encodedToken, string newPassword);

    Task<ProfileDto?> GetProfileAsync(string userId);
    Task<OperationResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
}
