-- ============================================================
-- Migracion 12: corrige el telefono de Lidon, permite un segundo
-- ayudante en las rutas planificadas (y que un chofer tambien pueda
-- ir como ayudante), y agrega la marca "Doblete".
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

-- Verificacion
SELECT RTRIM(Dni) AS Dni, Nombres, Apellidos, Telefono FROM Empleado WHERE Apellidos LIKE 'VELIZ%';
SELECT TOP 5 CodigoPlan, DniAyudante, DniAyudante2, EsDoblete FROM RutaPlanificada ORDER BY CodigoPlan DESC;
