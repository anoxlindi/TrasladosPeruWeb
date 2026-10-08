using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrasladosPeruWeb.Models;
using TrasladosPeruWeb.Repositories;
using TrasladosPeruWeb.Services;

namespace TrasladosPeruWeb.Controllers;

// Modulo "Planificar rutas": SOLO la administradora puede entrar.
[Authorize(Roles = "Administrador")]
public class PlanificacionController : Controller
{
    private readonly PlanificacionRepository _plan;
    private readonly RecorridoRepository _catalogos; // reutiliza las listas de choferes, ayudantes, unidades y clientes
    private readonly WhatsAppService _whatsapp;

    public PlanificacionController(PlanificacionRepository plan, RecorridoRepository catalogos, WhatsAppService whatsapp)
    {
        _plan = plan;
        _catalogos = catalogos;
        _whatsapp = whatsapp;
    }

    // GET /Planificacion -> lista de rutas armadas. Por defecto muestra desde hoy en adelante.
    public async Task<IActionResult> Index(FiltroPlanificacion filtro)
    {
        var sinFiltroEnLaUrl = !Request.Query.ContainsKey("FechaDesde") && !Request.Query.ContainsKey("FechaHasta");
        if (sinFiltroEnLaUrl)
        {
            filtro.FechaDesde = HoraPeru.Hoy;
        }

        var rutas = await _plan.ListarAsync(filtro);
        ViewBag.Filtro = filtro;
        ViewBag.WhatsAppAutomatico = _whatsapp.EstaConfigurado;
        return View(rutas);
    }

    // POST /Planificacion/EnviarWhatsApp -> envia el mensaje al chofer o a un ayudante sin salir de la pagina
    // destino: "chofer", "ayudante1" o "ayudante2"
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarWhatsApp(long id, string destino)
    {
        if (!_whatsapp.EstaConfigurado)
        {
            return Json(new { ok = false, mensaje = "El envío automático todavía no está configurado." });
        }

        var ruta = await _plan.ObtenerPorIdAsync(id);
        if (ruta is null)
        {
            return Json(new { ok = false, mensaje = "La ruta ya no existe." });
        }

        string? telefono = destino switch
        {
            "chofer" => ruta.TelefonoChofer,
            "ayudante1" => ruta.TelefonoAyudante,
            "ayudante2" => ruta.TelefonoAyudante2,
            _ => null
        };
        if (telefono is null)
        {
            return Json(new { ok = false, mensaje = "Esta ruta no tiene esa persona asignada." });
        }

        var (ok, detalle) = await _whatsapp.EnviarRutaAsync(telefono, MensajesRuta.Parametros(ruta, destino));
        return Json(new { ok, mensaje = ok ? "Enviado" : detalle });
    }

    // GET /Planificacion/Crear
    public async Task<IActionResult> Crear()
    {
        await CargarListasAsync();
        return View(new RutaPlanificadaForm { Fecha = HoraPeru.Hoy });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(RutaPlanificadaForm modelo)
    {
        Validar(modelo);
        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(modelo);
        }

        await _plan.CrearAsync(modelo);
        // Despues de guardar se muestra la lista desde el dia de la ruta, para ver y enviar los mensajes
        return RedirectToAction(nameof(Index), new
        {
            FechaDesde = modelo.Fecha.ToString("yyyy-MM-dd"),
            FechaHasta = modelo.Fecha.ToString("yyyy-MM-dd")
        });
    }

    // GET /Planificacion/Editar/5 (recibe el filtro actual para volver con el mismo)
    public async Task<IActionResult> Editar(long id, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var ruta = await _plan.ObtenerParaEditarAsync(id);
        if (ruta is null) return NotFound();

        ruta.FiltroFechaDesde = fechaDesde;
        ruta.FiltroFechaHasta = fechaHasta;

        await CargarListasAsync();
        return View(ruta);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(RutaPlanificadaForm modelo)
    {
        Validar(modelo);
        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(modelo);
        }

        await _plan.ActualizarAsync(modelo);
        return RedirectToAction(nameof(Index), new
        {
            FechaDesde = modelo.FiltroFechaDesde?.ToString("yyyy-MM-dd"),
            FechaHasta = modelo.FiltroFechaHasta?.ToString("yyyy-MM-dd")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(long id, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        await _plan.EliminarAsync(id);
        return RedirectToAction(nameof(Index), new
        {
            FechaDesde = fechaDesde?.ToString("yyyy-MM-dd"),
            FechaHasta = fechaHasta?.ToString("yyyy-MM-dd")
        });
    }

    // GET /Planificacion/ExportarExcel -> respeta el mismo rango Desde/Hasta de la pantalla
    public async Task<IActionResult> ExportarExcel(FiltroPlanificacion filtro)
    {
        var rutas = (await _plan.ListarAsync(filtro)).ToList();

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Rutas planificadas");

        var encabezados = new[]
        {
            "Codigo", "Fecha", "Salida de cochera", "Hora de cita", "Cliente", "Placa",
            "Doblete", "Chofer", "Telefono chofer", "Ayudante 1", "Telefono ayudante 1",
            "Ayudante 2", "Telefono ayudante 2",
            "Punto inicio", "Punto final", "Direccion", "Observaciones",
            "Salida viaje 2", "Hora cita viaje 2", "Cliente viaje 2",
            "Punto inicio viaje 2", "Punto final viaje 2", "Direccion viaje 2"
        };
        for (int i = 0; i < encabezados.Length; i++)
        {
            hoja.Cell(1, i + 1).Value = encabezados[i];
        }
        hoja.Row(1).Style.Font.Bold = true;

        int fila = 2;
        foreach (var r in rutas)
        {
            hoja.Cell(fila, 1).Value = r.CodigoPlan;
            hoja.Cell(fila, 2).Value = r.Fecha;
            hoja.Cell(fila, 3).Value = $"{r.HoraSalidaCochera.Hours:00}:{r.HoraSalidaCochera.Minutes:00}";
            hoja.Cell(fila, 4).Value = r.HoraCita.HasValue
                ? $"{r.HoraCita.Value.Hours:00}:{r.HoraCita.Value.Minutes:00}"
                : "-";
            hoja.Cell(fila, 5).Value = r.Cliente ?? "-";
            hoja.Cell(fila, 6).Value = r.Placa.Trim();
            hoja.Cell(fila, 7).Value = r.EsDoblete ? "Sí" : "No";
            hoja.Cell(fila, 8).Value = r.Chofer ?? "-";
            hoja.Cell(fila, 9).Value = r.TelefonoChofer?.Trim() ?? "-";
            hoja.Cell(fila, 10).Value = r.Ayudante ?? "-";
            hoja.Cell(fila, 11).Value = r.TelefonoAyudante?.Trim() ?? "-";
            hoja.Cell(fila, 12).Value = r.Ayudante2 ?? "-";
            hoja.Cell(fila, 13).Value = r.TelefonoAyudante2?.Trim() ?? "-";
            hoja.Cell(fila, 14).Value = r.PuntoInicio ?? "-";
            hoja.Cell(fila, 15).Value = r.PuntoFin ?? "-";
            hoja.Cell(fila, 16).Value = r.Direccion ?? "-";
            hoja.Cell(fila, 17).Value = r.Observaciones ?? "-";
            hoja.Cell(fila, 18).Value = r.HoraSalidaCochera2.HasValue
                ? $"{r.HoraSalidaCochera2.Value.Hours:00}:{r.HoraSalidaCochera2.Value.Minutes:00}" : "-";
            hoja.Cell(fila, 19).Value = r.HoraCita2.HasValue
                ? $"{r.HoraCita2.Value.Hours:00}:{r.HoraCita2.Value.Minutes:00}" : "-";
            hoja.Cell(fila, 20).Value = r.Cliente2 ?? "-";
            hoja.Cell(fila, 21).Value = r.PuntoInicio2 ?? "-";
            hoja.Cell(fila, 22).Value = r.PuntoFin2 ?? "-";
            hoja.Cell(fila, 23).Value = r.Direccion2 ?? "-";
            fila++;
        }
        hoja.Columns().AdjustToContents();
        hoja.RangeUsed()?.SetAutoFilter();

        using var stream = new MemoryStream();
        libro.SaveAs(stream);

        var nombreArchivo = $"Rutas_Planificadas_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            nombreArchivo);
    }

    private void Validar(RutaPlanificadaForm m)
    {
        if (!m.HoraSalidaCochera.HasValue)
        {
            ModelState.AddModelError("", "Indica la hora de salida de cochera.");
        }
        if (m.HoraCita.HasValue && m.HoraSalidaCochera.HasValue && m.HoraCita.Value < m.HoraSalidaCochera.Value)
        {
            ModelState.AddModelError("", "La hora de la cita no puede ser anterior a la salida de cochera.");
        }
        if (m.CodigoUnidad <= 0)
        {
            ModelState.AddModelError("", "Selecciona la unidad (camión).");
        }
        if (string.IsNullOrWhiteSpace(m.Ruc) && string.IsNullOrWhiteSpace(m.ClienteOtro))
        {
            ModelState.AddModelError("", "Selecciona un cliente, o escribe el nombre si es otro.");
        }

        // El chofer y los 2 ayudantes no pueden repetirse entre si
        var personas = new[] { m.DniChofer?.Trim(), m.DniAyudante?.Trim(), m.DniAyudante2?.Trim() }
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .ToList();
        if (personas.Count != personas.Distinct().Count())
        {
            ModelState.AddModelError("", "El chofer y los ayudantes no pueden repetirse en la misma ruta.");
        }

        // Si es doblete, el 2do viaje necesita sus propios datos (mismo chofer y unidad del primero)
        if (m.EsDoblete)
        {
            if (!m.HoraSalidaCochera2.HasValue)
            {
                ModelState.AddModelError("", "Indica la hora de salida del 2do viaje (doblete).");
            }
            if (m.HoraCita2.HasValue && m.HoraSalidaCochera2.HasValue && m.HoraCita2.Value < m.HoraSalidaCochera2.Value)
            {
                ModelState.AddModelError("", "La hora de la cita del 2do viaje no puede ser anterior a su salida de cochera.");
            }
            if (string.IsNullOrWhiteSpace(m.Ruc2) && string.IsNullOrWhiteSpace(m.ClienteOtro2))
            {
                ModelState.AddModelError("", "Selecciona el cliente del 2do viaje (doblete), o escribe el nombre si es otro.");
            }
            if (string.IsNullOrWhiteSpace(m.PuntoInicio2) || string.IsNullOrWhiteSpace(m.PuntoFin2))
            {
                ModelState.AddModelError("", "Indica el punto de inicio y de llegada del 2do viaje (doblete).");
            }
        }
    }

    private async Task CargarListasAsync()
    {
        ViewBag.Clientes = await _catalogos.ObtenerClientesAsync();
        ViewBag.Choferes = await _catalogos.ObtenerChoferesAsync();
        // El ayudante puede ser cualquier chofer o ayudante activo (un chofer tambien puede ir de ayudante)
        ViewBag.Ayudantes = await _catalogos.ObtenerPersonalAsync();
        ViewBag.Unidades = await _catalogos.ObtenerUnidadesAsync();
        ViewBag.Distritos = Distritos.Lista;
        // Solo para el punto de llegada: ademas de los distritos, la opcion "Varios / Reparto"
        ViewBag.DistritosDestino = Distritos.Lista.Append("Varios / Reparto").ToArray();
    }
}
