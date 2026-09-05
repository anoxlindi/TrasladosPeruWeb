-- ============================================================
-- INSERTS: DATOS MAESTROS (fijos)
-- ============================================================

-- ---------- EMPLEADO ----------
INSERT INTO Empleado (Dni, Nombres, Apellidos, Estado) VALUES
('07452723','CARLOS ALBERTO','AGUERO SARAVIA','Activo'),
('10625256','MAURICIO RICARDO','ALVARADO GONZALES','Activo'),
('77177456','ERINSON MIGUEL','ALVAREZ SALDANA','Activo'),
('80370132','JESUS DANIEL','BAUTISTA TORRES','Activo'),
('08447672','PEDRO CELESTINO','CARRANZA COCHACHIN','Activo'),
('08125115','JORGE EDGAR','CHAVEZ CORDOVA','Activo'),
('07258417','DAVID JESUS','CHUMBEZ RUIZ','Activo'),
('40866238','GINA ELIDA','CORZO ROBINET','Activo'),
('75436946','CARLOS OMAR','DIAZ MESIAS','Activo'),
('70844580','ERIC ORLANDO','GILES CAMACHO','Activo'),
('70298028','MAYKOL JUNIOR','GONZALES BARRIONUEVO','Activo'),
('15760432','ELIAS ERIC','GRADOS MORALES','Activo'),
('07462603','CARLOS DANIEL','GUTIERREZ CORRALES','Activo'),
('77066522','LUIS ENRIQUE','LEON POMA','Activo'),
('25849367','CARLOS ENRIQUE','NAVARRO BEUNZA','Activo'),
('40798839','CARLOS RAUL','PIZARRO QUIROZ','Activo'),
('44069776','JUNIOR ESTRA','RENGIFO OLIVEIRA','Activo'),
('41286766','LEONARDO','SAAVEDRA DANAQUIRI','Activo'),
('03682113','LUIS ALBERTO','SANDOVAL SILVA','Activo'),
('45232390','DANY JHIME','TAPIA RAFAYLE','Activo'),
('77501008','JEFERSON MARCELO','ULLOA RODRIGUEZ','Activo'),
('008912541','RICHARD JOSE','VEGA SOSA','Activo'),
('42859179','LIDON','VELIZ MERINO','Activo'),
('09613226','ALEJANDRO ALFREDO','VERASTIGUE FRANCISCO','Activo'),
('48129622','DIEGO FELIZARDO AGUSTO','REYNA FERRER','Activo'),
('005942014','WILSON EDUARDO','VARELA MONSERRATE','Activo'),
('74374197','BRUNO AXEL','VILCA ORDONEZ','Activo'),
('45989289','JAHAIRO JAMIR','SAENZ MESIAS','Activo');
GO

-- ---------- CONDUCTOR ----------
INSERT INTO Conductor (Dni) VALUES
('07452723'),
('77177456'),
('08125115'),
('07258417'),
('40866238'),
('07462603'),
('25849367'),
('40798839'),
('03682113'),
('45232390'),
('42859179'),
('48129622'),
('74374197'),
('45989289');
GO

-- ---------- ASISTENTE ----------
-- OJO: Cargo puesto por defecto como 'Ayudante de Carga' (no vino el detalle real por persona).
INSERT INTO Asistente (Dni, Cargo) VALUES
('10625256','Ayudante de Carga'),
('80370132','Ayudante de Carga'),
('08447672','Ayudante de Carga'),
('75436946','Ayudante de Carga'),
('70844580','Ayudante de Carga'),
('70298028','Ayudante de Carga'),
('15760432','Ayudante de Carga'),
('77066522','Ayudante de Carga'),
('44069776','Ayudante de Carga'),
('41286766','Ayudante de Carga'),
('77501008','Ayudante de Carga'),
('008912541','Ayudante de Carga'),
('09613226','Ayudante de Carga'),
('005942014','Ayudante de Carga');
GO

-- ---------- CAPACIDADUNIDAD ----------
-- Orden importa: define los CodigoCapacidad 1 al 5 usados abajo en UnidadTransporte.
INSERT INTO CapacidadUnidad (Toneladas, CantidadPaletas) VALUES
(17, 13),   -- CodigoCapacidad = 1
(9, 12),    -- CodigoCapacidad = 2
(5, 8),     -- CodigoCapacidad = 3
(4, 6),     -- CodigoCapacidad = 4
(1.5, 2);   -- CodigoCapacidad = 5
GO

-- ---------- UNIDADTRANSPORTE ----------
-- OJO: Modelo puesto como 'Por definir' (no vino en tu lista de placas).
INSERT INTO UnidadTransporte (TipoUnidad, Modelo, Placa, CodigoCapacidad) VALUES
('Furgon','Por definir','CBG 809',1),
('Furgon','Por definir','BEM 744',2),
('Furgon','Por definir','DOM 807',3),
('Furgon','Por definir','AUF 737',3),
('Furgon','Por definir','BKM 840',4),
('Furgon','Por definir','BEO-701',5),
('Furgon','Por definir','CBB 744',5),
('Furgon','Por definir','BHP 833',5),
('Furgon','Por definir','BVH 733',5),
('Furgon','Por definir','BXR 826',5),
('Furgon','Por definir','CJT-708',5),
('Furgon','Por definir','CJW-821',5),
('Furgon','Por definir','CNG-873',5);
GO

-- ---------- VERIFICACION ----------
SELECT * FROM Empleado;
SELECT * FROM Conductor;
SELECT * FROM Asistente;
SELECT * FROM CapacidadUnidad;
SELECT * FROM UnidadTransporte;

-- ============================================================
-- PENDIENTE (necesito estos datos para poder insertarlos):
--   1) Cliente: falta el RUC de cada una de las 17 empresas
--      (CULINARIA GROUP PERU SAC, AMTEX S.A.C, PRODUCTOS EXTRAGEL Y UNIVERSAL SAC,
--       BLUE OCEAN INVESTMENTS E.I.R.L., SOCIEDAD QUIMICA ALEMANA S.A., FAST PACKED S.A.C.,
--       ZONA DE AROMA SAC, CERSUR SUR PERU S.A., MULTEX E.I.R.L., QSI PERU SA,
--       DISTRIBUYA CORP S.A.C., DULCES IDEAS EIRL, STOCKHOLM MINING SAC,
--       SERVICIOS Y FORMULACIONES INDUSTRIALES S.A. (SERFI SA), SYNTHESIA TECHNOLOGY S.A.C.,
--       VINA TACAMA S.A, EMERGENT COLD SOLUCIONES INTEGRALES S.A.C.)
--   2) ConductorUnidad: NO se llena aqui, es variable (el asistente elige que chofer
--      maneja que unidad en cada viaje, desde la interfaz).
--   3) Reemplazar el placeholder de Modelo (UnidadTransporte) por el dato real.
-- ============================================================
