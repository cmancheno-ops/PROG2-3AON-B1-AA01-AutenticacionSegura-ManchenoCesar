using AutoTaller.Business;
using AutoTaller.Business.Abstractions;
using AutoTaller.Business.Identity;
using AutoTaller.Business.Models;
using AutoTaller.Business.Services;
using AutoTaller.Data;
using AutoTaller.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;
var sec = cfg.GetSection("Security");

// ---------- Capa de datos ----------
var connectionString = cfg.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection (use user-secrets o variables de entorno).");
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connectionString));

// ---------- Seguridad transversal: ASP.NET Core Identity ----------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(o =>
    {
        // Política de contraseña
        o.Password.RequiredLength = 10;
        o.Password.RequireDigit = true;
        o.Password.RequireLowercase = true;
        o.Password.RequireUppercase = true;
        o.Password.RequireNonAlphanumeric = true;
        o.Password.RequiredUniqueChars = 4;

        // Usuario y correo
        o.User.RequireUniqueEmail = true;

        // Bloqueo temporal por intentos fallidos
        o.Lockout.AllowedForNewUsers = true;
        o.Lockout.MaxFailedAccessAttempts = sec.GetValue("MaxFailedAccessAttempts", 5);
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(sec.GetValue("LockoutMinutes", 15));
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<SpanishIdentityErrorDescriber>()
    .AddDefaultTokenProviders();

// Token de recuperación con caducidad (un solo uso: ResetPasswordAsync rota el SecurityStamp).
builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
    o.TokenLifespan = TimeSpan.FromMinutes(sec.GetValue("ResetTokenMinutes", 30)));

// Revalidar el SecurityStamp en cada petición: logout / cambio de rol / deshabilitar surten efecto de inmediato.
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.Cookie.Name = "AutoTaller.Auth";
    o.Cookie.HttpOnly = true;
    // Producción: solo HTTPS. Desarrollo: permite http://localhost.
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.ExpireTimeSpan = TimeSpan.FromMinutes(sec.GetValue("CookieExpireMinutes", 30));
    o.SlidingExpiration = true;
});

// ---------- Capa de negocio ----------
builder.Services.Configure<SeedOptions>(cfg.GetSection("Seed"));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<DevEmailOutbox>();
    builder.Services.AddSingleton<IAppEmailSender, DevEmailSender>();   // correo simulado
}
else
{
    builder.Services.AddSingleton<IAppEmailSender, LogOnlyEmailSender>(); // reemplazar por SMTP real (secretos fuera del repo)
}

// ---------- Capa de presentación ----------
builder.Services.AddControllersWithViews(o =>
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));   // antiforgery en todos los POST

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

// Base de datos + roles + administrador inicial
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (db.Database.GetMigrations().Any()) db.Database.Migrate();   // si existen migraciones de EF
    else db.Database.EnsureCreated();                                // si no, crea el esquema (o usa el esquema del script SQL)
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();
