using Microsoft.AspNetCore.Mvc;

namespace AutoTaller.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    [ResponseCache(Duration = 0, NoStore = true)]
    public IActionResult Error() => View();
}
