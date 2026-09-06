USE TrasladosPeru2;
GO

IF COL_LENGTH('RecorridoTransporte', 'CostoAdicional') IS NULL
    ALTER TABLE RecorridoTransporte ADD CostoAdicional DECIMAL(10,2) NULL;
GO

CREATE OR ALTER VIEW vw_ReporteRecorridos AS
SELECT
    r.CodigoRecorrido,
    r.FechaInicio,
    r.FechaFin,
    r.KilometrajeInicial,
    r.KilometrajeFinal,
    r.KilometrajeRecorrido,
    r.Costo,
    r.CostoAdicional,
    r.DniChofer,
    r.DniAyudante,
    u.Placa,
    u.CodigoUnidad,
    COALESCE(cl.RazonSocial, r.ClienteOtro) AS Cliente,
    ec.Nombres + ' ' + ec.Apellidos AS Chofer,
    ea.Nombres + ' ' + ea.Apellidos AS Ayudante,
    ruta.PuntoInicio,
    ruta.PuntoFin
FROM RecorridoTransporte r
JOIN TransporteCargamento tc ON r.CodTransporteCargamento = tc.CodTransporteCargamento
JOIN UnidadTransporte u ON tc.CodigoUnidad = u.CodigoUnidad
JOIN Ruta ruta ON ruta.CodigoRuta = r.CodigoRuta
LEFT JOIN Solicitud s ON s.CodTransporteCargamento = tc.CodTransporteCargamento
LEFT JOIN Cliente cl ON cl.Ruc = s.Ruc
LEFT JOIN Empleado ec ON RTRIM(ec.Dni) = RTRIM(r.DniChofer)
LEFT JOIN Empleado ea ON RTRIM(ea.Dni) = RTRIM(r.DniAyudante);
GO