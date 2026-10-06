-- ============================================================
-- Migracion 11: Planificar rutas (modulo solo para la administradora)
--   1) Telefono de cada empleado (para enviarles su ruta por WhatsApp)
--   2) Tabla RutaPlanificada (la ruta armada con anticipacion)
--
-- IMPORTANTE: conectarse a la base de datos en Azure y ejecutar
-- TODO el script. No lleva USE (Azure no lo permite).
-- ============================================================

-- ------------------------------------------------------------
-- 1) Columna Telefono en Empleado
-- ------------------------------------------------------------
IF COL_LENGTH('Empleado', 'Telefono') IS NULL
    ALTER TABLE Empleado ADD Telefono CHAR(9) NULL;
GO

-- ------------------------------------------------------------
-- 2) Cargar los telefonos. El cruce se hace por valor numerico del DNI,
--    asi no importa si en la base tiene ceros a la izquierda o espacios.
-- ------------------------------------------------------------
DECLARE @Tel TABLE (Dni VARCHAR(12), Telefono CHAR(9));
INSERT INTO @Tel (Dni, Telefono) VALUES
('03682113', '986320321'),  -- Luis Alberto Sandoval Silva
('07258417', '939005878'),  -- David Jesus Chumbez Ruiz
('07452723', '944973091'),  -- Carlos Alberto Aguero Saravia
('07462603', '950128455'),  -- Carlos Daniel Gutierrez Corrales
('10625256', '937740228'),  -- Mauricio Ricardo Alvarado Gonzales
('25849367', '980665066'),  -- Carlos Enrique Navarro Beunza
('40866238', '946031313'),  -- Gina Elida Corzo Robinet
('42859179', '962313542'),  -- Lidon Veliz Merino
('45232390', '956948414'),  -- Dany Jhime Tapia Rafayle
('48129622', '993537395'),  -- Diego Felizardo Augusto Reyna Ferrer
('77177456', '910388162'),  -- Erinson Miguel Alvarez Saldana
('005942014', '901853021'), -- Wilson Eduardo Varela Monserrate
('008912541', '907824401'), -- Richard Jose Vega Sosa
('08447672', '997855993'),  -- Pedro Celestino Carranza Cochachin
('15760432', '967073339'),  -- Elias Eric Grados Morales
('41286766', '960064588'),  -- Leonardo Saavedra Danaquiri
('44069776', '957823198'),  -- Junior Estra Rengifo Oliveira
('70298028', '921678980'),  -- Maykol Junior Gonzales Barrionuevo
('70844580', '956518900'),  -- Eric Orlando Giles Camacho
('77501008', '914430415');  -- Jefferson Marcelo Ulloa Rodriguez

UPDATE e
SET e.Telefono = t.Telefono
FROM Empleado e
JOIN @Tel t ON TRY_CAST(RTRIM(e.Dni) AS BIGINT) = TRY_CAST(t.Dni AS BIGINT);

PRINT CONCAT('Telefonos cargados: ', @@ROWCOUNT, ' de 20.');

-- Si alguno no se encontro en Empleado, aparece aqui (deberia salir vacio)
SELECT t.Dni AS DniSinCoincidencia, t.Telefono
FROM @Tel t
WHERE NOT EXISTS (
    SELECT 1 FROM Empleado e
    WHERE TRY_CAST(RTRIM(e.Dni) AS BIGINT) = TRY_CAST(t.Dni AS BIGINT)
);
GO

-- ------------------------------------------------------------
-- 3) Tabla de rutas planificadas
-- ------------------------------------------------------------
IF OBJECT_ID('RutaPlanificada', 'U') IS NULL
BEGIN
    CREATE TABLE RutaPlanificada (
        CodigoPlan BIGINT IDENTITY(1,1) PRIMARY KEY,
        Fecha DATE NOT NULL,
        HoraSalidaCochera TIME(0) NOT NULL,
        HoraCita TIME(0) NULL,

        Ruc CHAR(11) NULL,
        ClienteOtro VARCHAR(60) NULL,

        CodigoUnidad BIGINT NOT NULL,
        DniChofer CHAR(9) NOT NULL,
        DniAyudante CHAR(9) NULL,

        PuntoInicio VARCHAR(60) NOT NULL,
        PuntoFin VARCHAR(60) NOT NULL,
        Direccion VARCHAR(150) NULL,
        Observaciones VARCHAR(300) NULL,

        FechaCreacion DATETIME NOT NULL DEFAULT GETDATE(),

        CONSTRAINT FK_Plan_Cliente  FOREIGN KEY (Ruc) REFERENCES Cliente(Ruc),
        CONSTRAINT FK_Plan_Unidad   FOREIGN KEY (CodigoUnidad) REFERENCES UnidadTransporte(CodigoUnidad),
        CONSTRAINT FK_Plan_Chofer   FOREIGN KEY (DniChofer) REFERENCES Conductor(Dni),
        CONSTRAINT FK_Plan_Ayudante FOREIGN KEY (DniAyudante) REFERENCES Asistente(Dni)
    );
END
GO

-- Verificacion
SELECT RTRIM(Dni) AS Dni, Nombres, Apellidos, Telefono FROM Empleado ORDER BY Apellidos;
SELECT COUNT(*) AS RutasPlanificadas FROM RutaPlanificada;