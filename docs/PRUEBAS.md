# Matriz de pruebas – AA01 Autenticación segura

Ejecutar con la app en `Development` (política de bloqueo: 3 intentos / 2 minutos).
Cuentas: ver README. Complete la columna **Evidencia** con la captura/minuto del video.

| N.º | Prueba | Pasos | Resultado esperado | ¿Cumple? | Evidencia |
|---|---|---|---|---|---|
| 1 | Registrar Cliente válido | `/Account/Register` con nombre, correo nuevo y contraseña `Prueba#Taller1` | Redirige a login; en SQL (`02_consultas_verificacion.sql`) aparece el usuario con rol **Cliente** y `PasswordHash` (no texto plano) | ☐ | |
| 2 | Correo duplicado / contraseña inválida | Registrar de nuevo el mismo correo; luego contraseña `abc123` | "Ese correo ya está registrado."; "debe tener al menos 10 caracteres…" – no se crea el usuario | ☐ | |
| 3 | Login válido y logout | Login con `cliente@autotaller.test`; abrir `/Personal`; pulsar *Cerrar sesión*; volver a `/Personal` | Entra al área; tras salir, redirige a `/Account/Login` | ☐ | |
| 4 | Contraseña incorrecta repetida | 3 intentos con clave errónea; luego intentar con la clave correcta | Mismo mensaje genérico en todos; la 4.ª (aun correcta) se rechaza durante 2 min (`LockoutEnd` en BD) | ☐ | |
| 5 | Recuperación con token válido | *Olvidé mi contraseña* → abrir `/Dev/Outbox` → enlace → nueva contraseña → login | Cambia la contraseña y permite iniciar sesión | ☐ | |
| 6 | Token inválido / caducado / reutilizado | (a) alterar un carácter del token; (b) reutilizar el enlace ya usado; (c) esperar `ResetTokenMinutes` | Mensaje "El enlace no es válido o ha caducado." en los tres casos | ☐ | |
| 7 | Cliente accede a URL administrativa | Con sesión de Cliente, pegar `/Admin/Users` | 403 "Acceso denegado" generado por el servidor (menú oculto no cuenta) | ☐ | |
| 8 | Admin cambia rol o estado | Login `admin@autotaller.test`; en `/Admin/Users` cambiar rol de un Cliente y deshabilitar otro | Se actualiza en pantalla y en BD; el usuario deshabilitado no puede entrar y su sesión abierta se invalida | ☐ | |
| 9 | Cliente intenta autoasignarse Administrador | Como Cliente, enviar POST a `/Admin/SetRole` (p. ej. con DevTools/curl) | Rechazado (403/redirección a AccessDenied); además el servicio revalida el rol del actor | ☐ | |
| 10 | Cambio de contraseña con clave actual incorrecta | `/Profile/ChangePassword` con actual errónea | "La contraseña actual es incorrecta." y no cambia | ☐ | |
| Extra | Última cuenta administradora | Como único admin, intentar quitarse el rol o deshabilitarse | Rechazado con mensaje explícito | ☐ | |
| Extra | Antiforgery | POST sin token (curl) a `/Account/Login` | 400 Bad Request | ☐ | |
