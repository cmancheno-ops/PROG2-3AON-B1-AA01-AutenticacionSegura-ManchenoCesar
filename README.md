# AA01 – Módulo integral de autenticación y seguridad (AutoTaller Seguro)

Programación II · ITSCO · Caso: Mantenimiento Automotriz.
Módulo de identidad en **ASP.NET Core 8 MVC + ASP.NET Core Identity + EF Core + SQL Server**.
Todos los datos son ficticios.

## Arquitectura en tres capas

| Capa | Proyecto | Contenido |
|---|---|---|
| Presentación | `src/AutoTaller.Web` | Controllers, Views Razor, ViewModels, `Program.cs`. **No** contienen reglas ni consultas: llaman a interfaces de negocio. |
| Negocio | `src/AutoTaller.Business` | `IAuthService` / `AuthService` (registro, login, recuperación, perfil), `IUserAdminService` (roles, habilitar/deshabilitar, regla de la última cuenta admin), `IdentitySeeder`. |
| Datos | `src/AutoTaller.Data` | `ApplicationUser`, `ApplicationDbContext : IdentityDbContext`, migraciones, SQL Server. |
| Seguridad transversal | Identity + cookies | `PasswordHasher` (PBKDF2 con sal), `UserManager`, `SignInManager`, `[Authorize(Roles=…)]`, antiforgery global. |

## Requisitos
- .NET SDK 8
- SQL Server (LocalDB, Express o Developer)
- Herramienta EF: `dotnet tool install --global dotnet-ef --version 8.0.11`

## Ejecución

```bash
# 1) Cadena de conexión (por defecto LocalDB con autenticación de Windows, sin credenciales).
#    Para otra instancia, NO edite appsettings.json: use user-secrets
cd src/AutoTaller.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\\SQLEXPRESS;Database=AutoTallerAuth;Trusted_Connection=True;TrustServerCertificate=True"

# 2) Base de datos – elija UNA opción:
#   A) Migraciones EF (recomendada)
cd ..\..            # raíz de la solución
dotnet ef migrations add InitialCreate -p src/AutoTaller.Data -s src/AutoTaller.Web
dotnet ef database update -p src/AutoTaller.Data -s src/AutoTaller.Web
#   B) Script SQL: ejecutar database/01_crear_base_y_esquema.sql en SSMS (y NO generar migraciones).
#   Si no hace ninguna, la app crea el esquema con EnsureCreated() al primer arranque.

# 3) Ejecutar
dotnet run --project src/AutoTaller.Web --launch-profile https
```

Al iniciar, la app crea los roles `Cliente` y `Administrador` y las cuentas de prueba (solo en **Development**).

## Usuarios de prueba (ficticios, solo Development)

| Rol | Correo | Contraseña |
|---|---|---|
| Administrador | `admin@autotaller.test` | `Admin#Taller2026` |
| Cliente | `cliente@autotaller.test` | `Cliente#Taller2026` |

Están en `appsettings.Development.json` porque son cuentas de demostración sin valor real.
**En producción** esos valores van vacíos y se inyectan por variables de entorno/secretos:
`Seed__AdminEmail`, `Seed__AdminPassword`, `ConnectionStrings__DefaultConnection`.
El registro público **nunca** asigna el rol Administrador.

## Política de seguridad implementada

- Contraseña: mínimo 10 caracteres, mayúscula, minúscula, número, símbolo, 4 caracteres distintos.
- Hash: `PasswordHasher` de Identity (PBKDF2-HMAC-SHA512 con sal aleatoria); nunca texto plano ni SHA-256 directo.
- Bloqueo temporal: Development 3 intentos / 2 min; producción 5 intentos / 15 min (`Security` en appsettings).
- Mensajes genéricos en login y en "olvidé mi contraseña" (no revelan si el correo existe).
- Recuperación: token de `DataProtectorTokenProvider` con caducidad (15 min dev / 30 min prod) y **un solo uso**
  (al restablecer cambia el `SecurityStamp`, lo que invalida el token).
- Logout rota el `SecurityStamp`: una cookie copiada deja de servir. `ValidationInterval = 0` hace que cambios de rol,
  deshabilitación y logout apliquen en la siguiente petición.
- Autorización en servidor: `[Authorize]` en áreas personales y `[Authorize(Roles="Administrador")]` en `AdminController`;
  `UserAdminService` vuelve a verificar que quien actúa es administrador activo (defensa en profundidad).
- No se puede quitar el rol ni deshabilitar a la **última cuenta administradora activa**.
- Antiforgery automático en todos los POST (`AutoValidateAntiforgeryTokenAttribute`).
- Cookie `HttpOnly`, `SameSite=Lax`, `Secure` obligatoria fuera de Development.
- El perfil usa siempre el id del claim de sesión (solo datos propios).

## Desarrollo vs. producción

| Aspecto | Desarrollo | Producción |
|---|---|---|
| Correo | Simulado en memoria, visible en `/Dev/Outbox` (404 fuera de Development) | Implementar `IAppEmailSender` con SMTP/API real; secretos en variables de entorno o Key Vault |
| Cookie | `SecurePolicy = SameAsRequest` | `Always` (solo HTTPS) + HSTS |
| Cuentas semilla | Demo en `appsettings.Development.json` | Solo por variables de entorno |
| Cadena de conexión | LocalDB / user-secrets | Variable de entorno, credenciales fuera del repo |
| Errores | Página detallada | `/Home/Error` sin detalles |

## Pruebas
Ver `docs/PRUEBAS.md` (los 10 casos mínimos + 2 extra) y `database/02_consultas_verificacion.sql`.
Guion del video en `docs/GUION_VIDEO.md`.

## Estructura
```
database/   scripts SQL
docs/       pruebas, guion de video, declaración de uso de IA
src/        Data · Business · Web
```
