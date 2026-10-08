using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TrasladosPeruWeb.Models;

namespace TrasladosPeruWeb.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    // A donde manda UseExceptionHandler cuando algo falla sin manejar.
    // Antes esta accion no existia, asi que cualquier error real quedaba
    // tapado por una pagina generica y fea en vez de mostrar algo util.
    [Route("/Home/Error")]
    public IActionResult Error()
    {
        var detalle = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        return View(new ErrorViewModel
        {
            RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            RutaOriginal = detalle?.Path,
            Mensaje = detalle?.Error.Message
        });
    }
}
