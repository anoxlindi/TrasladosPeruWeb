using Dapper;
using TrasladosPeruWeb.Data;
using TrasladosPeruWeb.Models;

namespace TrasladosPeruWeb.Repositories;

public class RecorridoRepository
{
    private readonly ConexionFactory _conexion;

    public RecorridoRepository(ConexionFactory conexion)
    {
        _conexion = conexion;
    }

    // Lista para la pantalla principal.
    // - Si esDniOperador tiene valor: fuerza a mostrar SOLO los viajes de HOY donde esa persona
    //   fue chofer o ayudante (regla para Chofer/Ayudante comunes).
    // - Si esDniOperador es null (administradora): usa los filtros opcionales que haya elegido.
    public async Task<IEnumerable<ReporteRecorridoDto>> ObtenerTodosAsync(string? esDniOperador, FiltroRecorridos? filtro = null)
    {
        using var db = _conexion.CrearConexion();
        var sql = @"SELECT CodigoRecorrido, FechaInicio, FechaFin,
                            KilometrajeInicial, KilometrajeFinal, KilometrajeRecorrido, Costo, CostoAdicional,
                            Placa, CodigoUnidad, DniChofer, DniAyudante, Cliente, Chofer, Ayudante,
                            PuntoInicio, PuntoFin
                     FROM vw_ReporteRecorridos
                     WHERE 1 = 1";

        var parametros = new DynamicParameters();

        if (esDniOperador is not null)
        {
            // Regla fija para chofer/ayudante: solo el dia de hoy, solo sus propios viajes
            sql += " AND CAST(FechaInicio AS DATE) = CAST(GETDATE() AS DATE)";
            sql += " AND (RTRIM(DniChofer) = @dni OR RTRIM(DniAyudante) = @dni)";
            parametros.Add("dni", esDniOperador);
        }
        else if (filtro is not null)
        {
            if (filtro.Fecha.HasValue)
            {
                sql += " AND CAST(FechaInicio AS DATE) = @fecha";
                parametros.Add("fecha", filtro.Fecha.Value.Date);
            }
            if (!string.IsNullOrWhiteSpace(filtro.DniChofer))
            {
                sql += " AND RTRIM(DniChofer) = @dniChofer";
                parametros.Add("dniChofer", filtro.DniChofer);
            }
            if (filtro.CodigoUnidad.HasValue)
            {
                sql += " AND CodigoUnidad = @codigoUnidad";
                parametros.Add("codigoUnidad", filtro.CodigoUnidad.Value);
            }
        }

        sql += " ORDER BY FechaInicio DESC";
        return await db.QueryAsync<ReporteRecorridoDto>(sql, parametros);
    }

    // ---------- Listas para los <select> del formulario "Nuevo viaje" ----------

    public async Task<IEnumerable<OpcionSelect>> ObtenerClientesAsync()
    {
        using var db = _conexion.CrearConexion();
        var sql = "SELECT Ruc AS Codigo, RazonSocial AS Texto FROM Cliente ORDER BY RazonSocial";
        return await db.QueryAsync<OpcionSelect>(sql);
    }

    public async Task<IEnumerable<OpcionSelect>> ObtenerChoferesAsync()
    {
        using var db = _conexion.CrearConexion();
        var sql = @"SELECT c.Dni AS Codigo, CONCAT(e.Nombres, ' ', e.Apellidos) AS Texto
                    FROM Conductor c JOIN Empleado e ON e.Dni = c.Dni
                    WHERE e.Estado = 'Activo'
                    ORDER BY e.Nombres";
        return await db.QueryAsync<OpcionSelect>(sql);
    }

    public async Task<IEnumerable<OpcionSelect>> ObtenerAyudantesAsync()
    {
        using var db = _conexion.CrearConexion();
        var sql = @"SELECT a.Dni AS Codigo, CONCAT(e.Nombres, ' ', e.Apellidos) AS Texto
                    FROM Asistente a JOIN Empleado e ON e.Dni = a.Dni
                    WHERE e.Estado = 'Activo'
                    ORDER BY e.Nombres";
        return await db.QueryAsync<OpcionSelect>(sql);
    }

    public async Task<IEnumerable<OpcionSelect>> ObtenerUnidadesAsync()
    {
        using var db = _conexion.CrearConexion();
        var sql = @"SELECT CAST(u.CodigoUnidad AS VARCHAR(20)) AS Codigo,
                           CONCAT(u.Placa, ' (', cu.Toneladas, ' TM - ', cu.CantidadPaletas, ' paletas)') AS Texto
                    FROM UnidadTransporte u
                    JOIN CapacidadUnidad cu ON cu.CodigoCapacidad = u.CodigoCapacidad
                    ORDER BY u.Placa";
        return await db.QueryAsync<OpcionSelect>(sql);
    }

    // Arma TODO el viaje en una sola transaccion: Cargamento, TransporteCargamento,
    // Ruta (nueva, una por viaje), Solicitud (si eligieron cliente) y el Recorrido final.
    public async Task CrearViajeCompletoAsync(NuevoViajeForm f)
    {
        using var db = _conexion.CrearConexion();
        db.Open();
        using var tx = db.BeginTransaction();
        try
        {
            var codigoCargamento = await db.ExecuteScalarAsync<long>(
                "INSERT INTO Cargamento (TipoCargamento, Peso) OUTPUT INSERTED.CodigoCargamento VALUES (@TipoCargamento, @Peso)",
                new { f.TipoCargamento, f.Peso }, tx);

            var codTransporteCargamento = await db.ExecuteScalarAsync<long>(
                "INSERT INTO TransporteCargamento (CodigoUnidad, CodigoCargamento) OUTPUT INSERTED.CodTransporteCargamento VALUES (@CodigoUnidad, @codigoCargamento)",
                new { f.CodigoUnidad, codigoCargamento }, tx);

            var codigoRuta = await db.ExecuteScalarAsync<long>(
                "INSERT INTO Ruta (Estado, PuntoInicio, PuntoFin) OUTPUT INSERTED.CodigoRuta VALUES ('Finalizada', @PuntoInicio, @PuntoFin)",
                new { f.PuntoInicio, f.PuntoFin }, tx);

            // Si eligio un cliente del catalogo (no "Otro"), se registra la Solicitud formal
            if (!string.IsNullOrWhiteSpace(f.Ruc))
            {
                await db.ExecuteAsync(
                    "INSERT INTO Solicitud (FechaSolicitud, Ruc, CodTransporteCargamento) VALUES (GETDATE(), @Ruc, @codTransporteCargamento)",
                    new { f.Ruc, codTransporteCargamento }, tx);
            }

            // DniAyudante vacio significa "el chofer fue solo" -> se guarda como NULL, no como texto vacio
            string? dniAyudante = string.IsNullOrWhiteSpace(f.DniAyudante) ? null : f.DniAyudante;
            string? clienteOtro = string.IsNullOrWhiteSpace(f.Ruc) ? f.ClienteOtro : null;

            await db.ExecuteAsync(
                @"INSERT INTO RecorridoTransporte
                    (FechaInicio, FechaFin, KilometrajeInicial, KilometrajeFinal, CodigoRuta, CodTransporteCargamento,
                     DniChofer, DniAyudante, Costo, CostoAdicional, ClienteOtro)
                  VALUES
                    (@FechaInicio, @FechaFin, @KilometrajeInicial, @KilometrajeFinal, @codigoRuta, @codTransporteCargamento,
                     @DniChofer, @dniAyudante, @Costo, @CostoAdicional, @clienteOtro)",
                new
                {
                    f.FechaInicio, f.FechaFin, f.KilometrajeInicial, f.KilometrajeFinal,
                    codigoRuta, codTransporteCargamento, f.DniChofer, dniAyudante, f.Costo, f.CostoAdicional, clienteOtro
                }, tx);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // Trae un solo viaje con todos sus datos, para la pantalla de editar costo
    public async Task<ReporteRecorridoDto?> ObtenerPorIdAsync(long codigoRecorrido)
    {
        using var db = _conexion.CrearConexion();
        var sql = @"SELECT CodigoRecorrido, FechaInicio, FechaFin,
                            KilometrajeInicial, KilometrajeFinal, KilometrajeRecorrido, Costo, CostoAdicional,
                            Placa, CodigoUnidad, DniChofer, DniAyudante, Cliente, Chofer, Ayudante,
                            PuntoInicio, PuntoFin
                     FROM vw_ReporteRecorridos
                     WHERE CodigoRecorrido = @codigoRecorrido";
        return await db.QueryFirstOrDefaultAsync<ReporteRecorridoDto>(sql, new { codigoRecorrido });
    }

    // Gina completa el costo y el costo adicional de un viaje ya creado por el chofer
    public async Task ActualizarCostoAsync(long codigoRecorrido, decimal? costo, decimal? costoAdicional)
    {
        using var db = _conexion.CrearConexion();
        var sql = "UPDATE RecorridoTransporte SET Costo = @costo, CostoAdicional = @costoAdicional WHERE CodigoRecorrido = @codigoRecorrido";
        await db.ExecuteAsync(sql, new { codigoRecorrido, costo, costoAdicional });
    }

    // Elimina un recorrido por su codigo (boton "Eliminar")
    public async Task EliminarAsync(long codigoRecorrido)
    {
        using var db = _conexion.CrearConexion();
        await db.ExecuteAsync("DELETE FROM RecorridoTransporte WHERE CodigoRecorrido = @codigoRecorrido",
            new { codigoRecorrido });
    }

    // Metricas para la hoja de resumen del Excel: km totales y cantidad de viajes por cliente
    public async Task<IEnumerable<dynamic>> ObtenerMetricasPorClienteAsync()
    {
        using var db = _conexion.CrearConexion();
        var sql = @"SELECT Cliente, COUNT(*) AS CantidadViajes, SUM(KilometrajeRecorrido) AS KmTotales
                    FROM vw_ReporteRecorridos
                    WHERE Cliente IS NOT NULL
                    GROUP BY Cliente
                    ORDER BY CantidadViajes DESC";
        return await db.QueryAsync(sql);
    }
}
