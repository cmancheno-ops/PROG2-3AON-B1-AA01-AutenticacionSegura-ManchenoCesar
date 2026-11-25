/* Consultas para evidenciar en el video (casos 1, 5, 8 y 9). */
USE [AutoTallerAuth];
GO
-- Usuarios, rol y estado. PasswordHash es un hash con sal, jamás la contraseña.
SELECT u.Email, u.NombreCompleto, r.Name AS Rol, u.IsActive,
       u.AccessFailedCount, u.LockoutEnd, LEFT(u.PasswordHash, 20) + '...' AS HashParcial
FROM dbo.AspNetUsers u
LEFT JOIN dbo.AspNetUserRoles ur ON ur.UserId = u.Id
LEFT JOIN dbo.AspNetRoles r ON r.Id = ur.RoleId
ORDER BY u.Email;

-- Cuántos administradores activos existen (regla de la última cuenta administradora)
SELECT COUNT(*) AS AdministradoresActivos
FROM dbo.AspNetUsers u
JOIN dbo.AspNetUserRoles ur ON ur.UserId = u.Id
JOIN dbo.AspNetRoles r ON r.Id = ur.RoleId AND r.Name = N'Administrador'
WHERE u.IsActive = 1;
