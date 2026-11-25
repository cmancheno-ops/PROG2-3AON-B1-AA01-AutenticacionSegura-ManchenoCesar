# AA01 – Módulo integral de autenticación y seguridad (AutoTaller Seguro)

Instituto Superior Tecnológico Cordillera · Programación II · Actividad autónoma AA01
Caso de estudio: Mantenimiento Automotriz · Estudiante: César Mancheno

Módulo de identidad en **ASP.NET Core 8 MVC + ASP.NET Core Identity + Entity Framework Core + SQL Server**:
registro, inicio y cierre de sesión, recuperación de cuenta, perfil, roles (Cliente / Administrador)
y control de acceso en el servidor. Todos los datos son ficticios.

**Estado:** la solución compila sin errores ni advertencias y se probó en local con SQL Server 2025 Express
(instancia `SQLEXPRESS`). Los casos de prueba están en [`docs/PRUEBAS.md`](docs/PRUEBAS.md).

## Arquitectura en tres capas

| Capa | Proyecto | Contenido |
|---|---|---|
| Presentación | `src/AutoTaller.Web` | Controllers, vistas Razor, ViewModels y `Program.cs`. Los controladores no tienen reglas ni consultas: llaman a interfaces de negocio. |
| Negocio | `src/AutoTaller.Business` | `IAuthService` / `AuthService` (registro, login, recuperación, perfil), `IUserAdminService` / `UserAdminService` (roles, habilitar/deshabilitar, regla de la última cuenta administradora) e `IdentitySeeder`. |
| Datos | `src/AutoTaller.Data` | `ApplicationUser`, `ApplicationDbContext : IdentityDbContext`, migraciones de EF Core y SQL Server. |
| Seguridad transversal | Identity + cookies | `PasswordHasher`, `UserManager`, `SignInManager`, `[Authorize(Roles = …)]` y antiforgery global. |

MVC organiza la presentación y no reemplaza la separación en capas: las reglas viven en `Business` y el acceso a datos en `Data`.

## Requisitos

- .NET SDK 8 (`dotnet --list-sdks` debe mostrar una versión `8.x`)
- SQL Server: LocalDB, Express o Developer (probado con SQL Server 2025 Express, instancia `SQLEXPRESS`)
- Opcional, para generar migraciones nuevas: `dotnet tool install --global dotnet-ef --version 8.0.11`

## Cómo ejecutar

Desde la raíz del repositorio:

```powershell
# 1) Compilar
dotnet build

# 2) Cadena de conexión (no se guarda en el repositorio: va en user-secrets)
cd src\AutoTaller.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\SQLEXPRESS;Database=AutoTallerAuth;Trusted_Connection=True;TrustServerCertificate=True"
cd ..\..

# 3) Ejecutar
dotnet run --project src/AutoTaller.Web --launch-profile https
```

- Si usa **LocalDB**, no necesita el paso 2: `appsettings.json` ya apunta a `(localdb)\MSSQLLocalDB` con autenticación de Windows y sin credenciales.
- Abra `https://localhost:7181`. Si el navegador avisa del certificado de desarrollo, ejecute una vez `dotnet dev-certs https --trust`.

### Base de datos

El repositorio incluye la migración `InitialCreate` (`src/AutoTaller.Data/Migrations`).
Al arrancar, la aplicación la aplica sola (`Database.Migrate()`), crea los roles y las cuentas de prueba.
También puede hacerlo manualmente:

```powershell
dotnet ef database update -p src/AutoTaller.Data -s src/AutoTaller.Web
```

Alternativa con SQL: [`database/01_crear_base_y_esquema.sql`](database/01_crear_base_y_esquema.sql) crea el esquema equivalente;
su último bloque registra la migración como aplicada y solo se ejecuta si usó el script en lugar de las migraciones.
[`database/02_consultas_verificacion.sql`](database/02_consultas_verificacion.sql) muestra usuarios, roles y hashes.

## Usuarios de prueba (ficticios, solo Development)

| Rol | Correo | Contraseña |
|---|---|---|
| Administrador | `admin@autotaller.test` | `Admin#Taller2026` |
| Cliente | `cliente@autotaller.test` | `Cliente#Taller2026` |

Son cuentas de demostración sin valor real y están en `appsettings.Development.json`. En producción esos valores van vacíos y se
inyectan por variables de entorno: `Seed__AdminEmail`, `Seed__AdminPassword` y `ConnectionStrings__DefaultConnection`.
El registro público **nunca** asigna el rol Administrador. Los usuarios que se registran desde la aplicación reciben el rol Cliente.

## Funcionalidades

| Funcionalidad | Dónde está |
|---|---|
| Registro (correo único, contraseña con política, confirmación) | `AccountController.Register` → `AuthService.RegisterAsync` |
| Login / logout con cookie, mensajes genéricos y bloqueo temporal | `AccountController.Login/Logout` → `AuthService` |
| Contraseñas con hash y sal | `PasswordHasher` de Identity (PBKDF2); nunca texto plano ni SHA-256 directo |
| Recuperación de contraseña (token con caducidad y de un solo uso) | `ForgotPassword` / `ResetPassword`; correo simulado en `/Dev/Outbox` |
| Perfil y cambio de contraseña (exige la actual) | `ProfileController` |
| Roles y administración de usuarios | `AdminController` → `UserAdminService` |
| Control de acceso | `[Authorize]` y `[Authorize(Roles = "Administrador")]`; página «Acceso denegado» con 403 |

## Política de seguridad implementada

- **Contraseña:** mínimo 10 caracteres, con mayúscula, minúscula, número, símbolo y 4 caracteres distintos.
- **Bloqueo temporal:** Development 3 intentos / 2 minutos; producción 5 intentos / 15 minutos (sección `Security` de appsettings).
- **Mensajes genéricos** en login y en «olvidé mi contraseña»: no revelan si el correo existe, si la cuenta está bloqueada o deshabilitada.
- **Recuperación:** el token de Identity caduca (15 min en desarrollo, 30 en producción) y solo sirve una vez, porque al restablecer cambia el `SecurityStamp`.
- **Sesión:** el logout rota el `SecurityStamp`, de modo que una cookie copiada deja de servir. `ValidationInterval = 0` hace que los cambios de rol, la deshabilitación y el logout apliquen en la siguiente petición.
- **Autorización en servidor:** `[Authorize(Roles = "Administrador")]` protege `AdminController`; `UserAdminService` vuelve a comprobar que quien actúa es administrador activo.
- **Última cuenta administradora:** no se puede quitar el rol ni deshabilitar al único administrador activo.
- **Antiforgery** automático en todos los POST (`AutoValidateAntiforgeryTokenAttribute`).
- **Cookie** `HttpOnly`, `SameSite=Lax` y `Secure` obligatoria fuera de Development.
- **Perfil:** usa siempre el id del claim de la sesión, nunca de la URL o del formulario.

## Desarrollo vs. producción

| Aspecto | Desarrollo | Producción |
|---|---|---|
| Correo | Simulado en memoria, visible en `/Dev/Outbox` (404 fuera de Development) | Implementar `IAppEmailSender` con SMTP/API real; secretos en variables de entorno o Key Vault |
| Cookie | `SecurePolicy = SameAsRequest` | `Always` (solo HTTPS) + HSTS |
| Cuentas semilla | Demo en `appsettings.Development.json` | Solo por variables de entorno |
| Cadena de conexión | LocalDB o user-secrets | Variable de entorno; credenciales fuera del repositorio |
| Bloqueo / token | 3 intentos, 2 min / 15 min | 5 intentos, 15 min / 30 min |
| Errores | Página detallada | `/Home/Error` sin detalles técnicos |

## Limitaciones conocidas

- El correo es simulado: sirve solo para demostración local. En producción hay que conectar un proveedor real.
- Bootstrap se carga desde un CDN, así que la interfaz necesita internet para verse con estilos.
- El cambio de contraseña desde el perfil no cuenta los fallos para el bloqueo por intentos.
- No se implementó la autenticación de dos factores ni el acceso con Google (mejoras opcionales según la consigna).

## Documentación y estructura

```
database/   script del esquema y consultas de verificación
docs/      declaración de uso de IA
src/        AutoTaller.Data · AutoTaller.Business · AutoTaller.Web
```

- [`docs/PRUEBAS.md`](docs/PRUEBAS.md): los 10 casos mínimos más 2 extra.
- `docs/AA01_Uso_de_IA_Prompts_y_Fuentes.pdf`: registro de herramientas de IA, prompts, verificaciones y fuentes.
