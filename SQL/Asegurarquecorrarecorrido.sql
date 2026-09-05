USE TrasladosPeru2;
GO

-- Asegurar que TODAS las columnas necesarias existan (no falla si ya existen)
IF COL_LENGTH('RecorridoTransporte', 'DniChofer') IS NULL
    ALTER TABLE RecorridoTransporte ADD DniChofer CHAR(9) NULL;

IF COL_LENGTH('RecorridoTransporte', 'DniAyudante') IS NULL
    ALTER TABLE RecorridoTransporte ADD DniAyudante CHAR(9) NULL;

IF COL_LENGTH('RecorridoTransporte', 'Costo') IS NULL
    ALTER TABLE RecorridoTransporte ADD Costo DECIMAL(10,2) NULL;

IF COL_LENGTH('RecorridoTransporte', 'CostoAdicional') IS NULL
    ALTER TABLE RecorridoTransporte ADD CostoAdicional DECIMAL(10,2) NULL;

IF COL_LENGTH('RecorridoTransporte', 'ClienteOtro') IS NULL
    ALTER TABLE RecorridoTransporte ADD ClienteOtro VARCHAR(60) NULL;

IF COL_LENGTH('Ruta', 'PuntoInicio') IS NULL
    ALTER TABLE Ruta ADD PuntoInicio VARCHAR(60) NULL;

IF COL_LENGTH('Ruta', 'PuntoFin') IS NULL
    ALTER TABLE Ruta ADD PuntoFin VARCHAR(60) NULL;

IF COL_LENGTH('Usuario', 'EsAdministrador') IS NULL
    ALTER TABLE Usuario ADD EsAdministrador BIT NOT NULL DEFAULT 0;
GO

-- Asegurar las FK de chofer/ayudante (si ya existen, este bloque las salta)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Recorrido_Chofer')
    ALTER TABLE RecorridoTransporte ADD CONSTRAINT FK_Recorrido_Chofer FOREIGN KEY (DniChofer) REFERENCES Conductor(Dni);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Recorrido_Ayudante')
    ALTER TABLE RecorridoTransporte ADD CONSTRAINT FK_Recorrido_Ayudante FOREIGN KEY (DniAyudante) REFERENCES Asistente(Dni);
GO

-- Gina como administradora
UPDATE Usuario SET EsAdministrador = 1 WHERE Dni = '40866238';
GO

-- Recrear la vista completa, con TODAS las columnas
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

-- Verificacion: esto DEBE funcionar sin error ahora
SELECT * FROM vw_ReporteRecorridos;
