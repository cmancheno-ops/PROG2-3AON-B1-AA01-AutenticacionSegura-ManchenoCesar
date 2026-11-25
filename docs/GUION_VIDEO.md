# Guion del video (máx. 8 min) – AA01

| Min | Qué mostrar | Qué decir |
|---|---|---|
| 0:00–0:30 | Título + estructura de carpetas | Caso, objetivo y 3 capas: **Data** (EF Core + Identity + SQL Server), **Business** (servicios), **Web** (MVC). Los controladores solo llaman a interfaces de negocio. |
| 0:30–1:30 | `Program.cs` | `AddIdentity`, política de contraseña, lockout, cookie HttpOnly/Secure, token con caducidad, antiforgery global, seeding. Dev vs. Producción (SecurePolicy, correo, secretos). |
| 1:30–2:30 | SQL Server (SSMS) | Tablas `AspNet*`; `PasswordHash` con hash+sal (PBKDF2 del `PasswordHasher`), no SHA-256 directo. |
| 2:30–3:30 | Pruebas 1 y 2 | Registro válido (rol Cliente en BD) y rechazos. |
| 3:30–4:15 | Pruebas 3 y 4 | Login/logout; bloqueo temporal con mensaje genérico. |
| 4:15–5:15 | Pruebas 5 y 6 | Recuperación con correo simulado (`/Dev/Outbox`); token alterado/reutilizado. Aclarar que la simulación es solo local. |
| 5:15–6:15 | Pruebas 7 y 9 | URL admin como Cliente → 403; intento de autoasignación. Explicar que ocultar el menú no es seguridad. |
| 6:15–7:15 | Prueba 8 + última cuenta admin | Cambio de rol/estado y aplicación inmediata (SecurityStamp). |
| 7:15–7:45 | Prueba 10 | Cambio de contraseña con clave actual incorrecta. |
| 7:45–8:00 | Cierre | README, `.gitignore`, secretos fuera del repo, PDF de IA. |
