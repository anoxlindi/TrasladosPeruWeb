-- ============================================================
-- Migracion 12: corrige el telefono de Lidon, permite un segundo
-- ayudante en las rutas planificadas (y que un chofer tambien pueda
-- ir como ayudante), agrega la marca "Doblete" y los datos del
-- segundo viaje (cuando la ruta es doblete).
--
-- IMPORTANTE: conectarse a la base de datos en Azure y ejecutar
-- TODO el script. No lleva USE (Azure no lo permite).
-- ============================================================

-- ------------------------------------------------------------
-- 1) Corrige el telefono de Lidon Veliz Merino (estaba mal: 962313542)
-- ------------------------------------------------------------
UPDATE Empleado
SET Telefono = '962319542'
WHERE TRY_CAST(RTRIM(Dni) AS BIGINT) = TRY_CAST('42859179' AS BIGINT);

PRINT CONCAT('Telefono de Lidon actualizado en ', @@ROWCOUNT, ' fila(s).');
GO

-- ------------------------------------------------------------
-- 2) Columna para un segundo ayudante y la marca "Doblete"
-- ------------------------------------------------------------
IF COL_LENGTH('RutaPlanificada', 'DniAyudante2') IS NULL
    ALTER TABLE RutaPlanificada ADD DniAyudante2 CHAR(9) NULL;

IF COL_LENGTH('RutaPlanificada', 'EsDoblete') IS NULL
    ALTER TABLE RutaPlanificada ADD EsDoblete BIT NOT NULL CONSTRAINT DF_Plan_Doblete DEFAULT (0);
GO

-- ------------------------------------------------------------
-- 3) El ayudante (1 y 2) ahora puede ser cualquier empleado activo,
--    no solo alguien registrado en la tabla Asistente, porque un
--    chofer tambien puede ir como ayudante en una ruta.
-- ------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Plan_Ayudante')
    ALTER TABLE RutaPlanificada DROP CONSTRAINT FK_Plan_Ayudante;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Plan_Ayudante')
    ALTER TABLE RutaPlanificada ADD CONSTRAINT FK_Plan_Ayudante FOREIGN KEY (DniAyudante) REFERENCES Empleado(Dni);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Plan_Ayudante2')
    ALTER TABLE RutaPlanificada ADD CONSTRAINT FK_Plan_Ayudante2 FOREIGN KEY (DniAyudante2) REFERENCES Empleado(Dni);
GO

-- ------------------------------------------------------------
-- 4) Datos del segundo viaje (solo se usan cuando EsDoblete = 1):
--    mismo cliente/horarios/puntos que el primero, pero del 2do viaje.
-- ------------------------------------------------------------
IF COL_LENGTH('RutaPlanificada', 'Ruc2') IS NULL
    ALTER TABLE RutaPlanificada ADD Ruc2 CHAR(11) NULL;

IF COL_LENGTH('RutaPlanificada', 'ClienteOtro2') IS NULL
    ALTER TABLE RutaPlanificada ADD ClienteOtro2 VARCHAR(60) NULL;

IF COL_LENGTH('RutaPlanificada', 'HoraSalidaCochera2') IS NULL
    ALTER TABLE RutaPlanificada ADD HoraSalidaCochera2 TIME(0) NULL;

IF COL_LENGTH('RutaPlanificada', 'HoraCita2') IS NULL
    ALTER TABLE RutaPlanificada ADD HoraCita2 TIME(0) NULL;

IF COL_LENGTH('RutaPlanificada', 'PuntoInicio2') IS NULL
    ALTER TABLE RutaPlanificada ADD PuntoInicio2 VARCHAR(60) NULL;

IF COL_LENGTH('RutaPlanificada', 'PuntoFin2') IS NULL
    ALTER TABLE RutaPlanificada ADD PuntoFin2 VARCHAR(60) NULL;

IF COL_LENGTH('RutaPlanificada', 'Direccion2') IS NULL
    ALTER TABLE RutaPlanificada ADD Direccion2 VARCHAR(150) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Plan_Cliente2')
    ALTER TABLE RutaPlanificada ADD CONSTRAINT FK_Plan_Cliente2 FOREIGN KEY (Ruc2) REFERENCES Cliente(Ruc);
GO

-- Verificacion
SELECT RTRIM(Dni) AS Dni, Nombres, Apellidos, Telefono FROM Empleado WHERE Apellidos LIKE 'VELIZ%';
SELECT TOP 5 CodigoPlan, DniAyudante, DniAyudante2, EsDoblete, Ruc2, PuntoInicio2, PuntoFin2
FROM RutaPlanificada ORDER BY CodigoPlan DESC;
