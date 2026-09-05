USE TrasladosPeru2;
GO

-- 1) Marca de administrador: solo Gina Corzo la tiene en 1
IF COL_LENGTH('Usuario', 'EsAdministrador') IS NULL
    ALTER TABLE Usuario ADD EsAdministrador BIT NOT NULL DEFAULT 0;
GO

UPDATE Usuario SET EsAdministrador = 1 WHERE Dni = '40866238';
GO
-- 2) Fix defensivo: usa RTRIM en el join para que el relleno de espacios de CHAR(9)
--    nunca impida que aparezca el nombre del chofer/ayudante.
CREATE OR ALTER VIEW vw_ReporteRecorridos AS
SELECT
    r.CodigoRecorrido,
    r.FechaInicio,
    r.FechaFin,
    r.KilometrajeInicial,
    r.KilometrajeFinal,
    r.KilometrajeRecorrido,
    r.Costo,
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

-- Verificacion rapida: revisa el registro de prueba que hiciste
SELECT CodigoRecorrido, DniChofer, DniAyudante FROM RecorridoTransporte;
