using AutoTaller.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoTaller.Web.Controllers;

/// <summary>Bandeja de correo simulado. Solo existe en Development: en otros entornos responde 404.</summary>
public class DevController : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly IServiceProvider _sp;
    public DevController(IWebHostEnvironment env, IServiceProvider sp) { _env = env; _sp = sp; }

    [HttpGet]
    public IActionResult Outbox()
    {
        var outbox = _env.IsDevelopment() ? _sp.GetService<DevEmailOutbox>() : null;
        if (outbox is null) return NotFound();
        return View(outbox.Items.Reverse().ToList());
    }
}
