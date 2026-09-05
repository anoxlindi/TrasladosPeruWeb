-- Correr esto UNA VEZ en tu base de datos TrasladosPeru2 (en SSMS o Azure Data Studio),
-- antes de correr el proyecto C#. Esta vista es la que usa la pagina web para mostrar
-- y exportar los recorridos con el nombre del cliente y la placa, no solo los codigos.

USE TrasladosPeru2;
GO

CREATE OR ALTER VIEW vw_ReporteRecorridos AS
SELECT
    r.CodigoRecorrido,
    r.FechaInicio,
    r.FechaFin,
    r.KilometrajeInicial,
    r.KilometrajeFinal,
    r.KilometrajeRecorrido,
    u.Placa,
    cl.RazonSocial AS Cliente
FROM RecorridoTransporte r
JOIN TransporteCargamento tc ON r.CodTransporteCargamento = tc.CodTransporteCargamento
JOIN UnidadTransporte u ON tc.CodigoUnidad = u.CodigoUnidad
LEFT JOIN Solicitud s ON s.CodTransporteCargamento = tc.CodTransporteCargamento
LEFT JOIN Cliente cl ON cl.Ruc = s.Ruc;
GO
