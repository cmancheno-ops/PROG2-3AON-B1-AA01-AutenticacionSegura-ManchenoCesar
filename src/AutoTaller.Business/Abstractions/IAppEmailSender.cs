namespace AutoTaller.Business.Abstractions;

/// <summary>Envío de correo. En desarrollo se simula (no sale ningún correo real).</summary>
public interface IAppEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody);
}
