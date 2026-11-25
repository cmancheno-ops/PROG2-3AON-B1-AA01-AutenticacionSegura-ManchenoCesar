using Microsoft.AspNetCore.Identity;

namespace AutoTaller.Data;

/// <summary>Usuario de la aplicación: extiende IdentityUser con datos propios.</summary>
public class ApplicationUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>false = cuenta deshabilitada por un administrador.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
