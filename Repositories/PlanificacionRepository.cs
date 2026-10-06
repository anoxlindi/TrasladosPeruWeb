using Dapper;
using TrasladosPeruWeb.Data;
using TrasladosPeruWeb.Models;

namespace TrasladosPeruWeb.Repositories;

public class PlanificacionRepository
{
    private readonly ConexionFactory _conexion;

    public PlanificacionRepository(ConexionFactory conexion)
    {
        _conexion = conexion;
    }

    private const string SelectBase = @"
        SELECT p.CodigoPlan, p.Fecha, p.HoraSalidaCochera, p.HoraCita,
               COALESCE(cl.RazonSocial, p.ClienteOtro) AS Cliente,
               u.Placa,
               p.DniChofer,
               ec.Nombres + ' ' + ec.Apellidos AS Chofer,
               ec.Telefono AS TelefonoChofer,
               p.DniAyudante,
               ea.Nombres + ' ' + ea.Apellidos AS Ayudante,
               ea.Telefono AS TelefonoAyudante,
               p.PuntoInicio, p.PuntoFin, p.Direccion, p.Observaciones
        FROM RutaPlanificada p
        JOIN UnidadTransporte u ON u.CodigoUnidad = p.CodigoUnidad
        LEFT JOIN Cliente cl ON cl.Ruc = p.Ruc
        LEFT JOIN Empleado ec ON ec.Dni = p.DniChofer
        LEFT JOIN Empleado ea ON ea.Dni = p.DniAyudante";

    public async Task<IEnumerable<RutaPlanificadaDto>> ListarAsync(FiltroPlanificacion filtro)
    {
        using var db = _conexion.CrearConexion();
        var sql = SelectBase + " WHERE 1 = 1";
        var parametros = new DynamicParameters();

        if (filtro.FechaDesde.HasValue)
        {
            sql += " AND p.Fecha >= @desde";
            parametros.Add("desde", filtro.FechaDesde.Value.Date);
        }
        if (filtro.FechaHasta.HasValue)
        {
            sql += " AND p.Fecha <= @hasta";
            parametros.Add("hasta", filtro.FechaHasta.Value.Date);
        }

        sql += " ORDER BY p.Fecha, p.HoraSalidaCochera, p.CodigoPlan";
        return await db.QueryAsync<RutaPlanificadaDto>(sql, parametros);
    }

    public async Task<RutaPlanificadaForm?> ObtenerParaEditarAsync(long codigoPlan)
    {
        using var db = _conexion.CrearConexion();
        var sql = @"SELECT CodigoPlan, Fecha, HoraSalidaCochera, HoraCita, Ruc, ClienteOtro,
                           CodigoUnidad, DniChofer, DniAyudante, PuntoInicio, PuntoFin,
                           Direccion, Observaciones
                    FROM RutaPlanificada
                    WHERE CodigoPlan = @codigoPlan";
        return await db.QueryFirstOrDefaultAsync<RutaPlanificadaForm>(sql, new { codigoPlan });
    }

    public async Task CrearAsync(RutaPlanificadaForm f)
    {
        using var db = _conexion.CrearConexion();
        var sql = @"INSERT INTO RutaPlanificada
                        (Fecha, HoraSalidaCochera, HoraCita, Ruc, ClienteOtro, CodigoUnidad,
                         DniChofer, DniAyudante, PuntoInicio, PuntoFin, Direccion, Observaciones)
                    VALUES
                        (@Fecha, @HoraSalidaCochera, @HoraCita, @Ruc, @ClienteOtro, @CodigoUnidad,
                         @DniChofer, @DniAyudante, @PuntoInicio, @PuntoFin, @Direccion, @Observaciones)";
        await db.ExecuteAsync(sql, ParametrosDe(f));
    }

    public async Task ActualizarAsync(RutaPlanificadaForm f)
    {
        using var db = _conexion.CrearConexion();
        var sql = @"UPDATE RutaPlanificada SET
                        Fecha = @Fecha, HoraSalidaCochera = @HoraSalidaCochera, HoraCita = @HoraCita,
                        Ruc = @Ruc, ClienteOtro = @ClienteOtro, CodigoUnidad = @CodigoUnidad,
                        DniChofer = @DniChofer, DniAyudante = @DniAyudante,
                        PuntoInicio = @PuntoInicio, PuntoFin = @PuntoFin,
                        Direccion = @Direccion, Observaciones = @Observaciones
                    WHERE CodigoPlan = @CodigoPlan";
        await db.ExecuteAsync(sql, ParametrosDe(f));
    }

    public async Task EliminarAsync(long codigoPlan)
    {
        using var db = _conexion.CrearConexion();
        await db.ExecuteAsync("DELETE FROM RutaPlanificada WHERE CodigoPlan = @codigoPlan", new { codigoPlan });
    }

    // Normaliza los textos vacios a NULL (cliente del catalogo vs "otro", sin ayudante, etc.)
    private static DynamicParameters ParametrosDe(RutaPlanificadaForm f)
    {
        var p = new DynamicParameters();
        var usaCatalogo = !string.IsNullOrWhiteSpace(f.Ruc);

        p.Add("CodigoPlan", f.CodigoPlan);
        p.Add("Fecha", f.Fecha.Date);
        p.Add("HoraSalidaCochera", f.HoraSalidaCochera);
        p.Add("HoraCita", f.HoraCita);
        p.Add("Ruc", usaCatalogo ? f.Ruc : null);
        p.Add("ClienteOtro", usaCatalogo ? null : f.ClienteOtro?.Trim());
        p.Add("CodigoUnidad", f.CodigoUnidad);
        p.Add("DniChofer", f.DniChofer);
        p.Add("DniAyudante", string.IsNullOrWhiteSpace(f.DniAyudante) ? null : f.DniAyudante);
        p.Add("PuntoInicio", f.PuntoInicio);
        p.Add("PuntoFin", f.PuntoFin);
        p.Add("Direccion", string.IsNullOrWhiteSpace(f.Direccion) ? null : f.Direccion.Trim());
        p.Add("Observaciones", string.IsNullOrWhiteSpace(f.Observaciones) ? null : f.Observaciones.Trim());
        return p;
    }
}
