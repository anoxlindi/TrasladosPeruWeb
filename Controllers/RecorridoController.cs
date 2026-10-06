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

    // GET /Recorrido  -> pantalla principal. Gina (admin) ve todo + filtros (rango de fechas). El resto, solo su dia.
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
        var ahora = DateTime.Now;
        var ahoraSinSegundos = new DateTime(ahora.Year, ahora.Month, ahora.Day, ahora.Hour, ahora.Minute, 0);
        return View(new NuevoViajeForm { FechaInicio = ahoraSinSegundos, FechaFin = ahoraSinSegundos.AddHours(2) });
    }

    // POST /Recorrido/Crear -> arma Cargamento + TransporteCargamento + Ruta + Solicitud + Recorrido
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(NuevoViajeForm modelo)
    {
        // Segun el rol REAL de la persona (no asumir que todos son chofer):
        // si es Chofer, se fija a si mismo en DniChofer. Si es Ayudante, se fija en DniAyudante
        // y elige libremente quien fue el chofer.
        if (!User.IsInRole("Administrador"))
        {
            var miDni = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            var miRol = await _repo.ObtenerRolAsync(miDni);

            if (miRol == "Ayudante")
            {
                modelo.DniAyudante = miDni;
            }
            else
            {
                modelo.DniChofer = miDni;
            }
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
        if (modelo.FuePeaje && (modelo.CantidadPeajes < 1 || modelo.CantidadPeajes > 4))
        {
            ModelState.AddModelError("", "La cantidad de peajes debe estar entre 1 y 4.");
        }
        if (modelo.FueLineaAmarilla && (modelo.CantidadLineaAmarilla < 1 || modelo.CantidadLineaAmarilla > 4))
        {
            ModelState.AddModelError("", "La cantidad de línea amarilla debe estar entre 1 y 4.");
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
        ViewBag.Distritos = Distritos.Lista;
        ViewBag.EsAdmin = User.IsInRole("Administrador");
        ViewBag.MiDni = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        ViewBag.MiNombre = User.Identity?.Name;
        ViewBag.MiRol = ViewBag.EsAdmin ? null : await _repo.ObtenerRolAsync((string)ViewBag.MiDni ?? "");
    }

    // GET /Recorrido/Editar/5 -> Gina edita cualquier campo de un viaje ya creado.
    // Recibe el filtro actual (fechaDesde/fechaHasta/dniChofer/codigoUnidad) como query string,
    // para poder reaplicarlo cuando Guardar vuelva a Index (asi el filtro no se resetea).
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Editar(long id, DateTime? fechaDesde, DateTime? fechaHasta, string? dniChofer, long? codigoUnidad)
    {
        var viaje = await _repo.ObtenerParaEditarAsync(id);
        if (viaje is null) return NotFound();

        // Se guarda el filtro actual dentro del propio formulario (campos ocultos)
        viaje.FiltroFechaDesde = fechaDesde;
        viaje.FiltroFechaHasta = fechaHasta;
        viaje.FiltroDniChofer = dniChofer;
        viaje.FiltroCodigoUnidad = codigoUnidad;

        await CargarListasEdicionAsync();
        return View(viaje);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(NuevoViajeForm modelo)
    {
        if (modelo.KilometrajeFinal <= modelo.KilometrajeInicial)
        {
            ModelState.AddModelError("", "El kilometraje final debe ser mayor al inicial.");
        }
        if (modelo.FechaFin <= modelo.FechaInicio)
        {
            ModelState.AddModelError("", "La fecha final debe ser posterior a la fecha de inicio.");
        }
        if (modelo.FuePeaje && (modelo.CantidadPeajes < 1 || modelo.CantidadPeajes > 4))
        {
            ModelState.AddModelError("", "La cantidad de peajes debe estar entre 1 y 4.");
        }
        if (modelo.FueLineaAmarilla && (modelo.CantidadLineaAmarilla < 1 || modelo.CantidadLineaAmarilla > 4))
        {
            ModelState.AddModelError("", "La cantidad de línea amarilla debe estar entre 1 y 4.");
        }

        if (!ModelState.IsValid)
        {
            await CargarListasEdicionAsync();
            return View(modelo);
        }

        await _repo.ActualizarViajeCompletoAsync(modelo);

        // Vuelve a Index reaplicando el mismo filtro que tenia antes de entrar a Editar
        return RedirectToAction(nameof(Index), new
        {
            FechaDesde = modelo.FiltroFechaDesde,
            FechaHasta = modelo.FiltroFechaHasta,
            DniChofer = modelo.FiltroDniChofer,
            CodigoUnidad = modelo.FiltroCodigoUnidad
        });
    }

    private async Task CargarListasEdicionAsync()
    {
        ViewBag.Clientes = await _repo.ObtenerClientesAsync();
        ViewBag.Choferes = await _repo.ObtenerChoferesAsync();
        ViewBag.Ayudantes = await _repo.ObtenerAyudantesAsync();
        ViewBag.Unidades = await _repo.ObtenerUnidadesAsync();
        ViewBag.TiposCarga = new[] { "Carga Fria", "Carga Seca", "MAPTEL", "Peligroso" };
        ViewBag.Distritos = Distritos.Lista;
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

    // GET /Recorrido/ExportarExcel -> SOLO Gina (administradora). Respeta el mismo filtro
    // (incluyendo rango Desde/Hasta) que esta aplicado en la pantalla, en vez de exportar todo.
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportarExcel(FiltroRecorridos filtro)
    {
        var recorridos = (await _repo.ObtenerTodosAsync(esDniOperador: null, filtro)).ToList();
        var metricas = (await _repo.ObtenerMetricasPorClienteAsync()).ToList();

        using var libro = new XLWorkbook();

        // Hoja 1: el detalle de todos los recorridos (segun el filtro aplicado)
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
        hojaDetalle.Cell(1, 13).Value = "Km Inicio Cochera";
        hojaDetalle.Cell(1, 14).Value = "Km Final Cochera";
        hojaDetalle.Cell(1, 15).Value = "Costo (S/)";
        hojaDetalle.Cell(1, 16).Value = "Costo adicional (S/)";
        hojaDetalle.Cell(1, 17).Value = "¿Peaje?";
        hojaDetalle.Cell(1, 18).Value = "Cant. Peajes";
        hojaDetalle.Cell(1, 19).Value = "Costo Peajes (S/)";
        hojaDetalle.Cell(1, 20).Value = "¿Línea amarilla?";
        hojaDetalle.Cell(1, 21).Value = "Cant. Línea amarilla";
        hojaDetalle.Cell(1, 22).Value = "Costo Línea amarilla (S/)";
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
            hojaDetalle.Cell(fila, 13).Value = r.KilometrajeInicioCochera ?? 0;
            hojaDetalle.Cell(fila, 14).Value = r.KilometrajeFinalCochera ?? 0;
            hojaDetalle.Cell(fila, 15).Value = r.Costo ?? 0;
            hojaDetalle.Cell(fila, 16).Value = r.CostoAdicional ?? 0;
            hojaDetalle.Cell(fila, 17).Value = r.FuePeaje ? "Sí" : "No";
            hojaDetalle.Cell(fila, 18).Value = r.CantidadPeajes;
            hojaDetalle.Cell(fila, 19).Value = r.CostoPeajes;
            hojaDetalle.Cell(fila, 20).Value = r.FueLineaAmarilla ? "Sí" : "No";
            hojaDetalle.Cell(fila, 21).Value = r.CantidadLineaAmarilla;
            hojaDetalle.Cell(fila, 22).Value = r.CostoLineaAmarilla;
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