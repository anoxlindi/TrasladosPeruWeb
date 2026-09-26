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

    // Averigua si la persona es Chofer o Ayudante de verdad (no asumir que todos son chofer)
    public async Task<string?> ObtenerRolAsync(string dni)
    {
        using var db = _conexion.CrearConexion();
        var esChofer = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Conductor WHERE RTRIM(Dni) = @dni", new { dni });
        if (esChofer > 0) return "Chofer";

        var esAyudante = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Asistente WHERE RTRIM(Dni) = @dni", new { dni });
        if (esAyudante > 0) return "Ayudante";

        return null;
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

    // Trae un viaje con TODOS sus campos editables, para la pantalla de edicion de Gina
    public async Task<NuevoViajeForm?> ObtenerParaEditarAsync(long codigoRecorrido)
    {
        using var db = _conexion.CrearConexion();
        var sql = @"SELECT
                        r.CodigoRecorrido, r.FechaInicio, r.FechaFin, r.KilometrajeInicial, r.KilometrajeFinal,
                        r.Costo, r.CostoAdicional, r.DniChofer, r.DniAyudante, r.ClienteOtro,
                        tc.CodigoUnidad, c.TipoCargamento, c.Peso,
                        s.Ruc, ruta.PuntoInicio, ruta.PuntoFin
                    FROM RecorridoTransporte r
                    JOIN TransporteCargamento tc ON tc.CodTransporteCargamento = r.CodTransporteCargamento
                    JOIN Cargamento c ON c.CodigoCargamento = tc.CodigoCargamento
                    JOIN Ruta ruta ON ruta.CodigoRuta = r.CodigoRuta
                    LEFT JOIN Solicitud s ON s.CodTransporteCargamento = tc.CodTransporteCargamento
                    WHERE r.CodigoRecorrido = @codigoRecorrido";
        return await db.QueryFirstOrDefaultAsync<NuevoViajeForm>(sql, new { codigoRecorrido });
    }

    // Gina edita cualquier campo de un viaje ya creado (en vez de borrar y crear uno nuevo)
    public async Task ActualizarViajeCompletoAsync(NuevoViajeForm f)
    {
        using var db = _conexion.CrearConexion();
        db.Open();
        using var tx = db.BeginTransaction();
        try
        {
            // Necesitamos los codigos internos (Ruta, TransporteCargamento, Cargamento) de este viaje
            var claves = await db.QueryFirstAsync(
                @"SELECT r.CodigoRuta, r.CodTransporteCargamento, tc.CodigoCargamento
                  FROM RecorridoTransporte r
                  JOIN TransporteCargamento tc ON tc.CodTransporteCargamento = r.CodTransporteCargamento
                  WHERE r.CodigoRecorrido = @id",
                new { id = f.CodigoRecorrido }, tx);

            await db.ExecuteAsync(
                "UPDATE Cargamento SET TipoCargamento = @TipoCargamento, Peso = @Peso WHERE CodigoCargamento = @CodigoCargamento",
                new { f.TipoCargamento, f.Peso, claves.CodigoCargamento }, tx);

            await db.ExecuteAsync(
                "UPDATE TransporteCargamento SET CodigoUnidad = @CodigoUnidad WHERE CodTransporteCargamento = @CodTransporteCargamento",
                new { f.CodigoUnidad, claves.CodTransporteCargamento }, tx);

            await db.ExecuteAsync(
                "UPDATE Ruta SET PuntoInicio = @PuntoInicio, PuntoFin = @PuntoFin WHERE CodigoRuta = @CodigoRuta",
                new { f.PuntoInicio, f.PuntoFin, claves.CodigoRuta }, tx);

            // Cliente: se borra la Solicitud anterior (si habia) y se crea de nuevo segun lo elegido ahora
            await db.ExecuteAsync("DELETE FROM Solicitud WHERE CodTransporteCargamento = @CodTransporteCargamento",
                new { claves.CodTransporteCargamento }, tx);

            string? clienteOtro = string.IsNullOrWhiteSpace(f.Ruc) ? f.ClienteOtro : null;
            if (!string.IsNullOrWhiteSpace(f.Ruc))
            {
                await db.ExecuteAsync(
                    "INSERT INTO Solicitud (FechaSolicitud, Ruc, CodTransporteCargamento) VALUES (GETDATE(), @Ruc, @CodTransporteCargamento)",
                    new { f.Ruc, claves.CodTransporteCargamento }, tx);
            }

            string? dniAyudante = string.IsNullOrWhiteSpace(f.DniAyudante) ? null : f.DniAyudante;

            await db.ExecuteAsync(
                @"UPDATE RecorridoTransporte SET
                    FechaInicio = @FechaInicio, FechaFin = @FechaFin,
                    KilometrajeInicial = @KilometrajeInicial, KilometrajeFinal = @KilometrajeFinal,
                    DniChofer = @DniChofer, DniAyudante = @dniAyudante,
                    Costo = @Costo, CostoAdicional = @CostoAdicional, ClienteOtro = @clienteOtro
                  WHERE CodigoRecorrido = @CodigoRecorrido",
                new
                {
                    f.FechaInicio, f.FechaFin, f.KilometrajeInicial, f.KilometrajeFinal,
                    f.DniChofer, dniAyudante, f.Costo, f.CostoAdicional, clienteOtro, f.CodigoRecorrido
                }, tx);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
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
