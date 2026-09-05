using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrasladosPeruWeb.Models;
using TrasladosPeruWeb.Repositories;

namespace TrasladosPeruWeb.Controllers;

[Authorize]
public class RecorridoController : Controller
{
    private readonly RecorridoRepository _repo;

    public RecorridoController(RecorridoRepository repo)
    {
        _repo = repo;
    }

    // GET /Recorrido  -> pantalla principal. Gina (admin) ve todo + filtros. El resto, solo su dia.
    public async Task<IActionResult> Index(FiltroRecorridos filtro)
    {
        var esAdmin = User.IsInRole("Administrador");
        var miDni = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var recorridos = esAdmin
            ? await _repo.ObtenerTodosAsync(esDniOperador: null, filtro)
            : await _repo.ObtenerTodosAsync(esDniOperador: miDni);

        ViewBag.EsAdmin = esAdmin;
        if (esAdmin)
        {
            ViewBag.Choferes = await _repo.ObtenerChoferesAsync();
            ViewBag.Unidades = await _repo.ObtenerUnidadesAsync();
            ViewBag.Filtro = filtro;
        }

        return View(recorridos);
    }

    // GET /Recorrido/Crear -> muestra el formulario completo
    public async Task<IActionResult> Crear()
    {
        await CargarListasAsync();
        return View(new NuevoViajeForm());
    }

    // POST /Recorrido/Crear -> arma Cargamento + TransporteCargamento + Ruta + Solicitud + Recorrido
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(NuevoViajeForm modelo)
    {
        // Un operador (no administrador) siempre queda como el chofer del viaje que registra,
        // sin importar que venga o no en el formulario (los campos disabled no se envian).
        if (!User.IsInRole("Administrador"))
        {
            modelo.DniChofer = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            modelo.Costo = null; // solo Gina puede fijar el costo del servicio
            modelo.CostoAdicional = null; // solo Gina puede fijar el costo adicional
        }

        if (string.IsNullOrWhiteSpace(modelo.DniChofer))
        {
            ModelState.AddModelError("", "Debes seleccionar un chofer.");
        }
        if (modelo.KilometrajeFinal <= modelo.KilometrajeInicial)
        {
            ModelState.AddModelError("", "El kilometraje final debe ser mayor al inicial.");
        }
        if (modelo.FechaFin <= modelo.FechaInicio)
        {
            ModelState.AddModelError("", "La fecha final debe ser posterior a la fecha de inicio.");
        }

        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(modelo);
        }

        await _repo.CrearViajeCompletoAsync(modelo);
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarListasAsync()
    {
        ViewBag.Clientes = await _repo.ObtenerClientesAsync();
        ViewBag.Choferes = await _repo.ObtenerChoferesAsync();
        ViewBag.Ayudantes = await _repo.ObtenerAyudantesAsync();
        ViewBag.Unidades = await _repo.ObtenerUnidadesAsync();
        ViewBag.TiposCarga = new[] { "Carga Fria", "Carga Seca", "MAPTEL", "Peligroso" };
        ViewBag.EsAdmin = User.IsInRole("Administrador");
        ViewBag.MiDni = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        ViewBag.MiNombre = User.Identity?.Name;
    }

    // GET /Recorrido/EditarCosto/5 -> Gina completa costo y costo adicional de un viaje ya creado
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EditarCosto(long id)
    {
        var viaje = await _repo.ObtenerPorIdAsync(id);
        if (viaje is null) return NotFound();
        return View(viaje);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarCosto(long id, decimal? Costo, decimal? CostoAdicional)
    {
        await _repo.ActualizarCostoAsync(id, Costo, CostoAdicional);
        return RedirectToAction(nameof(Index));
    }

    // POST /Recorrido/Eliminar/5 -> boton "Eliminar" de cada fila
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(long id)
    {
        var esAdmin = User.IsInRole("Administrador");
        if (!esAdmin)
        {
            // Un operador solo puede borrar sus propios viajes del dia de hoy
            var miDni = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var misViajesDeHoy = await _repo.ObtenerTodosAsync(esDniOperador: miDni);
            if (!misViajesDeHoy.Any(v => v.CodigoRecorrido == id))
            {
                return Forbid();
            }
        }

        await _repo.EliminarAsync(id);
        return RedirectToAction(nameof(Index));
    }

    // GET /Recorrido/ExportarExcel -> SOLO Gina (administradora)
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportarExcel(FiltroRecorridos filtro)
    {
        var recorridos = (await _repo.ObtenerTodosAsync(esDniOperador: null, filtro)).ToList();
        var metricas = (await _repo.ObtenerMetricasPorClienteAsync()).ToList();

        using var libro = new XLWorkbook();

        // Hoja 1: el detalle de todos los recorridos
        var hojaDetalle = libro.Worksheets.Add("Recorridos");
        hojaDetalle.Cell(1, 1).Value = "Codigo";
        hojaDetalle.Cell(1, 2).Value = "Placa";
        hojaDetalle.Cell(1, 3).Value = "Chofer";
        hojaDetalle.Cell(1, 4).Value = "Ayudante";
        hojaDetalle.Cell(1, 5).Value = "Cliente";
        hojaDetalle.Cell(1, 6).Value = "Ruta inicio";
        hojaDetalle.Cell(1, 7).Value = "Ruta fin";
        hojaDetalle.Cell(1, 8).Value = "Fecha Inicio";
        hojaDetalle.Cell(1, 9).Value = "Fecha Fin";
        hojaDetalle.Cell(1, 10).Value = "Km Inicial";
        hojaDetalle.Cell(1, 11).Value = "Km Final";
        hojaDetalle.Cell(1, 12).Value = "Km Recorridos";
        hojaDetalle.Cell(1, 13).Value = "Costo (S/)";
        hojaDetalle.Cell(1, 14).Value = "Costo adicional (S/)";
        hojaDetalle.Row(1).Style.Font.Bold = true;

        int fila = 2;
        foreach (var r in recorridos)
        {
            hojaDetalle.Cell(fila, 1).Value = r.CodigoRecorrido;
            hojaDetalle.Cell(fila, 2).Value = r.Placa;
            hojaDetalle.Cell(fila, 3).Value = r.Chofer ?? "-";
            hojaDetalle.Cell(fila, 4).Value = r.Ayudante ?? "-";
            hojaDetalle.Cell(fila, 5).Value = r.Cliente ?? "-";
            hojaDetalle.Cell(fila, 6).Value = r.PuntoInicio ?? "-";
            hojaDetalle.Cell(fila, 7).Value = r.PuntoFin ?? "-";
            hojaDetalle.Cell(fila, 8).Value = r.FechaInicio;
            hojaDetalle.Cell(fila, 9).Value = r.FechaFin;
            hojaDetalle.Cell(fila, 10).Value = r.KilometrajeInicial;
            hojaDetalle.Cell(fila, 11).Value = r.KilometrajeFinal;
            hojaDetalle.Cell(fila, 12).Value = r.KilometrajeRecorrido;
            hojaDetalle.Cell(fila, 13).Value = r.Costo ?? 0;
            hojaDetalle.Cell(fila, 14).Value = r.CostoAdicional ?? 0;
            fila++;
        }
        hojaDetalle.Columns().AdjustToContents();
        hojaDetalle.RangeUsed()?.SetAutoFilter();

        // Hoja 2: metricas por cliente (para responder "que cliente pidio mas viajes")
        var hojaMetricas = libro.Worksheets.Add("Metricas por Cliente");
        hojaMetricas.Cell(1, 1).Value = "Cliente";
        hojaMetricas.Cell(1, 2).Value = "Cantidad de Viajes";
        hojaMetricas.Cell(1, 3).Value = "Km Totales";
        hojaMetricas.Row(1).Style.Font.Bold = true;

        int filaM = 2;
        foreach (var m in metricas)
        {
            hojaMetricas.Cell(filaM, 1).Value = (string)m.Cliente;
            hojaMetricas.Cell(filaM, 2).Value = (int)m.CantidadViajes;
            hojaMetricas.Cell(filaM, 3).Value = Convert.ToDouble(m.KmTotales ?? 0);
            filaM++;
        }
        hojaMetricas.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        libro.SaveAs(stream);
        var contenido = stream.ToArray();

        var nombreArchivo = $"Reporte_Recorridos_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(contenido,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            nombreArchivo);
    }
}
