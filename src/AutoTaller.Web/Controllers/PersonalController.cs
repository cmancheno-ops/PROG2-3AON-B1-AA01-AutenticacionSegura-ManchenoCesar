using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTaller.Web.Controllers;

/// <summary>Área personal: accesible a cualquier usuario autenticado (Cliente o Administrador).</summary>
[Authorize]
public class PersonalController : Controller
{
    public IActionResult Index() => View();
}
