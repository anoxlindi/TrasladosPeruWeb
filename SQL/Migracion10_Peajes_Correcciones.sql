-- ============================================================
-- Migracion 10: Peajes, Linea amarilla, correcciones de unidades,
-- kilometraje de cochera, filtro por rango de fechas.
-- ============================================================
USE TrasladosPeru2;
GO

-- ------------------------------------------------------------
-- 1) Corregir placa mal escrita: CNG-873 -> CNH-873
-- ------------------------------------------------------------
UPDATE UnidadTransporte SET Placa = 'CNH-873' WHERE Placa = 'CNG-873';
GO

-- ------------------------------------------------------------
-- 2) Eliminar las 2 unidades que no existen en la realidad: DOM 807 y BKM 840.
--    Se usa TRY/CATCH porque si ya tienen viajes registrados (FK en
--    TransporteCargamento), el DELETE fallaria; en ese caso se desactivan
--    en vez de borrarse (no se pierde el historico de viajes ya hechos).
-- ------------------------------------------------------------
BEGIN TRY
    DELETE FROM UnidadTransporte WHERE Placa IN ('DOM 807', 'BKM 840');
    PRINT 'Unidades DOM 807 y BKM 840 eliminadas correctamente.';
END TRY
BEGIN CATCH
    PRINT 'No se pudieron eliminar (tienen viajes registrados). Se desactivan en su lugar.';
    IF COL_LENGTH('UnidadTransporte', 'Activo') IS NULL
        ALTER TABLE UnidadTransporte ADD Activo BIT NOT NULL DEFAULT 1;
    UPDATE UnidadTransporte SET Activo = 0 WHERE Placa IN ('DOM 807', 'BKM 840');
END CATCH
GO

-- ------------------------------------------------------------
-- 3) Tarifa de peaje / linea amarilla: se guarda POR PLACA (cada unidad
--    tiene su propia tarifa, segun la tabla que diste), no por tonelaje,
--    porque la tarifa depende de la unidad especifica, no solo de su categoria.
-- ------------------------------------------------------------
IF COL_LENGTH('UnidadTransporte', 'TarifaPeaje') IS NULL
    ALTER TABLE UnidadTransporte ADD TarifaPeaje DECIMAL(10,2) NOT NULL DEFAULT 0;
GO

-- Tarifas segun la tabla proporcionada (misma tarifa aplica a Peaje y a Linea amarilla)
UPDATE UnidadTransporte SET TarifaPeaje = 18.90 WHERE Placa IN ('CBG 809');
UPDATE UnidadTransporte SET TarifaPeaje = 12.60 WHERE Placa IN ('BEM 744', 'AUF 737');
UPDATE UnidadTransporte SET TarifaPeaje = 6.30  WHERE Placa IN
    ('BEO-701', 'CBB 744', 'BHP 833', 'BVH 733', 'BXR 826', 'CJT-708', 'CJW-821', 'CNH-873');
GO

-- ------------------------------------------------------------
-- 4) Kilometraje de cochera: ademas del kilometraje "del servicio" que ya
--    existia (KilometrajeInicial/KilometrajeFinal), se agrega el kilometraje
--    de inicio y fin de cochera (el recorrido completo, incluyendo ida/vuelta
--    a cochera).
-- ------------------------------------------------------------
IF COL_LENGTH('RecorridoTransporte', 'KilometrajeInicioCochera') IS NULL
    ALTER TABLE RecorridoTransporte ADD KilometrajeInicioCochera INT NULL;
IF COL_LENGTH('RecorridoTransporte', 'KilometrajeFinalCochera') IS NULL
    ALTER TABLE RecorridoTransporte ADD KilometrajeFinalCochera INT NULL;
GO

-- ------------------------------------------------------------
-- 5) Peaje: Si paso o no, y cuantos peajes (1 a 4). El costo NO se guarda
--    tipeado por el chofer: se calcula en el sistema (CantidadPeajes * TarifaPeaje
--    de la unidad usada) y se expone a traves de la vista.
-- ------------------------------------------------------------
IF COL_LENGTH('RecorridoTransporte', 'FuePeaje') IS NULL
    ALTER TABLE RecorridoTransporte ADD FuePeaje BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('RecorridoTransporte', 'CantidadPeajes') IS NULL
    ALTER TABLE RecorridoTransporte ADD CantidadPeajes INT NOT NULL DEFAULT 0;
GO

-- ------------------------------------------------------------
-- 6) Linea amarilla: mismo patron (Si/No + cantidad 1 a 4), misma tabla de tarifas.
-- ------------------------------------------------------------
IF COL_LENGTH('RecorridoTransporte', 'FueLineaAmarilla') IS NULL
    ALTER TABLE RecorridoTransporte ADD FueLineaAmarilla BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('RecorridoTransporte', 'CantidadLineaAmarilla') IS NULL
    ALTER TABLE RecorridoTransporte ADD CantidadLineaAmarilla INT NOT NULL DEFAULT 0;
GO

-- Constraints de rango (0 a 4) para las cantidades
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Recorrido_CantidadPeajes')
    ALTER TABLE RecorridoTransporte ADD CONSTRAINT CK_Recorrido_CantidadPeajes
        CHECK (CantidadPeajes BETWEEN 0 AND 4);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Recorrido_CantidadLineaAmarilla')
    ALTER TABLE RecorridoTransporte ADD CONSTRAINT CK_Recorrido_CantidadLineaAmarilla
        CHECK (CantidadLineaAmarilla BETWEEN 0 AND 4);
GO

-- ------------------------------------------------------------
-- 7) Vista actualizada: agrega todos los campos nuevos y calcula
--    CostoPeajes / CostoLineaAmarilla automaticamente segun la tarifa de la unidad.
-- ------------------------------------------------------------
CREATE OR ALTER VIEW vw_ReporteRecorridos AS
SELECT
    r.CodigoRecorrido,
    r.FechaInicio,
    r.FechaFin,
    r.KilometrajeInicial,
    r.KilometrajeFinal,
    r.KilometrajeRecorrido,
    r.KilometrajeInicioCochera,
    r.KilometrajeFinalCochera,
    r.Costo,
    r.CostoAdicional,
    u.Placa,
    u.CodigoUnidad,
    COALESCE(cl.RazonSocial, r.ClienteOtro) AS Cliente,
    ec.Nombres + ' ' + ec.Apellidos AS Chofer,
    ea.Nombres + ' ' + ea.Apellidos AS Ayudante,
    r.DniChofer,
    r.DniAyudante,
    ruta.PuntoInicio,
    ruta.PuntoFin,
    r.FuePeaje,
    r.CantidadPeajes,
    CAST(r.CantidadPeajes * u.TarifaPeaje AS DECIMAL(10,2)) AS CostoPeajes,
    r.FueLineaAmarilla,
    r.CantidadLineaAmarilla,
    CAST(r.CantidadLineaAmarilla * u.TarifaPeaje AS DECIMAL(10,2)) AS CostoLineaAmarilla
FROM RecorridoTransporte r
JOIN TransporteCargamento tc ON r.CodTransporteCargamento = tc.CodTransporteCargamento
JOIN UnidadTransporte u ON tc.CodigoUnidad = u.CodigoUnidad
JOIN Ruta ruta ON ruta.CodigoRuta = r.CodigoRuta
LEFT JOIN Solicitud s ON s.CodTransporteCargamento = tc.CodTransporteCargamento
LEFT JOIN Cliente cl ON cl.Ruc = s.Ruc
LEFT JOIN Empleado ec ON ec.Dni = r.DniChofer
LEFT JOIN Empleado ea ON ea.Dni = r.DniAyudante;
GO

-- ------------------------------------------------------------
-- 8) Olvide mi contraseña: no necesita columnas nuevas, usa las mismas
--    columnas de Usuario (Password, FechaUltimoCambio) que ya existian.
--    El sistema ahora lo hace solo, reemplazando el script manual
--    "Resetear_Password.sql" que usabas antes.
-- ------------------------------------------------------------

-- Verificacion final
SELECT Placa, TarifaPeaje FROM UnidadTransporte ORDER BY Placa;
SELECT CodigoRecorrido, KilometrajeInicioCochera, KilometrajeFinalCochera,
       FuePeaje, CantidadPeajes, FueLineaAmarilla, CantidadLineaAmarilla
FROM RecorridoTransporte;
