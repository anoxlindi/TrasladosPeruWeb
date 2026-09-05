using Microsoft.AspNetCore.Mvc;

namespace TrasladosPeruWeb.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
