using System.Collections.Concurrent;
using AutoTaller.Business.Abstractions;

namespace AutoTaller.Web.Services;

public record SimulatedEmail(DateTime AtUtc, string To, string Subject, string HtmlBody);

/// <summary>Bandeja simulada en memoria. SOLO se registra en Development.</summary>
public class DevEmailOutbox
{
    public ConcurrentQueue<SimulatedEmail> Items { get; } = new();
}

public class DevEmailSender : IAppEmailSender
{
    private readonly DevEmailOutbox _outbox;
    private readonly ILogger<DevEmailSender> _log;
    public DevEmailSender(DevEmailOutbox outbox, ILogger<DevEmailSender> log) { _outbox = outbox; _log = log; }

    public Task SendAsync(string to, string subject, string htmlBody)
    {
        _outbox.Items.Enqueue(new SimulatedEmail(DateTime.UtcNow, to, subject, htmlBody));
        _log.LogInformation("[CORREO SIMULADO] Para {To}: {Subject} (ver /Dev/Outbox)", to, subject);
        return Task.CompletedTask;
    }
}

/// <summary>Producción por defecto: no expone tokens en logs. Sustituir por proveedor SMTP/API real.</summary>
public class LogOnlyEmailSender : IAppEmailSender
{
    private readonly ILogger<LogOnlyEmailSender> _log;
    public LogOnlyEmailSender(ILogger<LogOnlyEmailSender> log) => _log = log;

    public Task SendAsync(string to, string subject, string htmlBody)
    {
        _log.LogWarning("Proveedor de correo no configurado: no se envió '{Subject}'.", subject);
        return Task.CompletedTask;
    }
}
