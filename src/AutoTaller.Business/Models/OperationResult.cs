namespace AutoTaller.Business.Models;

public class OperationResult
{
    public bool Succeeded { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = Array.Empty<string>();

    public static OperationResult Ok() => new() { Succeeded = true };
    public static OperationResult Fail(params string[] errors) => new() { Succeeded = false, Errors = errors };
    public static OperationResult Fail(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors.ToArray() };
}

public enum LoginStatus { Success, Failed, LockedOut }

public record RegisterRequest(string NombreCompleto, string Email, string Password);

public record ProfileDto(string Id, string NombreCompleto, string Email, string Rol, DateTime CreatedAtUtc);

public record UserListItem(string Id, string NombreCompleto, string Email, string Rol, bool IsActive, bool LockedOut);

public class SeedOptions
{
    public string? AdminEmail { get; set; }
    public string? AdminPassword { get; set; }
    public string AdminName { get; set; } = "Administrador Inicial";
    public string? DemoClientEmail { get; set; }
    public string? DemoClientPassword { get; set; }
}
