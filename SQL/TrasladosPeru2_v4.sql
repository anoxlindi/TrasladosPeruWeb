-- ============================================================
-- BASE DE DATOS: TrasladosPeru2
-- Version 4:
--   1) Dni ampliado a CHAR(9) en Empleado/Conductor/Asistente/ManipulacionCargamento/ConductorUnidad
--      (habia 2 personas con Carne de Extranjeria de 9 digitos)
--   2) Nombres y Apellidos ampliados a VARCHAR(25) (un nombre tenia 22 caracteres)
--   3) Inserts de datos maestros: Empleado, Conductor, Asistente, CapacidadUnidad, UnidadTransporte
--   4) Cliente y ConductorUnidad quedan pendientes (falta RUC y asignacion chofer-unidad)
-- ============================================================

CREATE DATABASE TrasladosPeru2;
GO
USE TrasladosPeru2;
GO

CREATE TABLE Empleado (
    Dni CHAR(9) PRIMARY KEY,
    Nombres VARCHAR(25) NOT NULL,
    Apellidos VARCHAR(25) NOT NULL,
    Estado VARCHAR(10) NOT NULL
);

CREATE TABLE Conductor (
    Dni CHAR(9) PRIMARY KEY,

    CONSTRAINT FK_Conductor_Empleado
        FOREIGN KEY (Dni)
        REFERENCES Empleado(Dni)
);

CREATE TABLE Asistente (
    Dni CHAR(9) PRIMARY KEY,
    Cargo VARCHAR(20) NOT NULL,

    CONSTRAINT FK_Asistente_Empleado
        FOREIGN KEY (Dni)
        REFERENCES Empleado(Dni)
);

-- Catalogo de capacidad por unidad (toneladas <-> cantidad de paletas)
CREATE TABLE CapacidadUnidad (
    CodigoCapacidad INT IDENTITY(1,1) PRIMARY KEY,
    Toneladas DECIMAL(5,2) UNIQUE NOT NULL,
    CantidadPaletas INT NOT NULL
);

CREATE TABLE UnidadTransporte (
    CodigoUnidad BIGINT IDENTITY(1,1) PRIMARY KEY,
    TipoUnidad VARCHAR(15)  NOT NULL,
    Modelo VARCHAR(15)  NOT NULL,
    Placa CHAR(7) UNIQUE  NOT NULL,

    CodigoCapacidad INT NOT NULL,

    CONSTRAINT FK_Unidad_Capacidad
        FOREIGN KEY (CodigoCapacidad)
        REFERENCES CapacidadUnidad(CodigoCapacidad)
);

CREATE TABLE ConductorUnidad (
    Dni CHAR(9),
    CodigoUnidad BIGINT,

    CONSTRAINT PK_ConductorUnidad
        PRIMARY KEY (Dni, CodigoUnidad),

    CONSTRAINT FK_CU_Conductor
        FOREIGN KEY (Dni)
        REFERENCES Conductor(Dni),

    CONSTRAINT FK_CU_Unidad
        FOREIGN KEY (CodigoUnidad)
        REFERENCES UnidadTransporte(CodigoUnidad)
);

CREATE TABLE Cargamento (
    CodigoCargamento BIGINT IDENTITY(1,1) PRIMARY KEY,
    TipoCargamento VARCHAR(20)  NOT NULL,
    Peso DECIMAL(10,2)  NOT NULL
);

CREATE TABLE ManipulacionCargamento (
    Dni CHAR(9),
    CodigoCargamento BIGINT,

    FechaRegistro DATETIME  NOT NULL,
    TipoManipulacion VARCHAR(20)  NOT NULL,
    TiempoTomado VARCHAR(20)  NOT NULL,

    CONSTRAINT PK_ManipulacionCargamento
        PRIMARY KEY (Dni, CodigoCargamento),

    CONSTRAINT FK_MC_Asistente
        FOREIGN KEY (Dni)
        REFERENCES Asistente(Dni),

    CONSTRAINT FK_MC_Cargamento
        FOREIGN KEY (CodigoCargamento)
        REFERENCES Cargamento(CodigoCargamento)
);

-- Solo Ruc y RazonSocial. PENDIENTE: aun no se insertan datos, falta el RUC real de cada cliente.
CREATE TABLE Cliente (
    Ruc CHAR(11) PRIMARY KEY,
    RazonSocial VARCHAR(45)  NOT NULL
);

CREATE TABLE Ruta (
    Estado VARCHAR(15)  NOT NULL
);

CREATE TABLE PuntoRuta (
    CodigoPunto BIGINT IDENTITY(1,1) PRIMARY KEY,
    Direccion VARCHAR(40)  NOT NULL,
    TipoPunto VARCHAR(20)  NOT NULL,
    Orden INT  NOT NULL,

    CodigoRuta BIGINT NOT NULL,

    CONSTRAINT FK_PuntoRuta_Ruta
        FOREIGN KEY (CodigoRuta)
        REFERENCES Ruta(CodigoRuta)
);

CREATE TABLE TransporteCargamento (
    CodTransporteCargamento BIGINT IDENTITY(1,1) PRIMARY KEY,

    CodigoUnidad BIGINT NOT NULL,
    CodigoCargamento BIGINT NOT NULL,

    CONSTRAINT FK_TC_Unidad
        FOREIGN KEY (CodigoUnidad)
        REFERENCES UnidadTransporte(CodigoUnidad),

    CONSTRAINT FK_TC_Cargamento
        FOREIGN KEY (CodigoCargamento)
        REFERENCES Cargamento(CodigoCargamento)
);

CREATE TABLE Solicitud (
    CodigoSolicitud BIGINT IDENTITY(1,1) PRIMARY KEY,
    FechaSolicitud DATETIME  NOT NULL,

    Ruc CHAR(11) NOT NULL,
    CodTransporteCargamento BIGINT NOT NULL,

    CONSTRAINT FK_Solicitud_Cliente
        FOREIGN KEY (Ruc)
        REFERENCES Cliente(Ruc),

    CONSTRAINT FK_Solicitud_Transporte
        FOREIGN KEY (CodTransporteCargamento)
        REFERENCES TransporteCargamento(CodTransporteCargamento)
);

CREATE TABLE RecorridoTransporte (
    CodigoRecorrido BIGINT IDENTITY(1,1) PRIMARY KEY,

    FechaInicio DATETIME  NOT NULL,
    FechaFin DATETIME  NOT NULL,

    KilometrajeInicial INT NOT NULL,
    KilometrajeFinal INT NOT NULL,
    KilometrajeRecorrido AS (KilometrajeFinal - KilometrajeInicial),

    CodigoRuta BIGINT NOT NULL,
    CodTransporteCargamento BIGINT NOT NULL,

    CONSTRAINT FK_Recorrido_Ruta
        FOREIGN KEY (CodigoRuta)
        REFERENCES Ruta(CodigoRuta),

    CONSTRAINT FK_Recorrido_Transporte
        FOREIGN KEY (CodTransporteCargamento)
        REFERENCES TransporteCargamento(CodTransporteCargamento)
);

-- ============================================================
-- CHECK CONSTRAINTS
-- ============================================================

ALTER TABLE Empleado
    ADD CONSTRAINT CK_Empleado_Estado
        CHECK (Estado IN ('Activo', 'Inactivo'));

ALTER TABLE Empleado
    ADD CONSTRAINT CK_Empleado_Dni
        CHECK (Dni NOT LIKE '%[^0-9]%');

ALTER TABLE CapacidadUnidad
    ADD CONSTRAINT CK_CapacidadUnidad_Toneladas
        CHECK (Toneladas IN (17, 9, 5, 4, 1.5));

ALTER TABLE CapacidadUnidad
    ADD CONSTRAINT CK_CapacidadUnidad_Paletas
        CHECK (CantidadPaletas > 0);

ALTER TABLE UnidadTransporte
    ADD CONSTRAINT CK_Unidad_TipoUnidad
        CHECK (TipoUnidad IN ('Furgon'));

ALTER TABLE Cargamento
    ADD CONSTRAINT CK_Cargamento_Peso
        CHECK (Peso > 0);

ALTER TABLE Cargamento
    ADD CONSTRAINT CK_Cargamento_Tipo
        CHECK (TipoCargamento IN ('Carga Fria', 'Carga Seca', 'MAPTEL', 'Peligroso'));

ALTER TABLE Ruta
    ADD CONSTRAINT CK_Ruta_Estado
        CHECK (Estado IN ('Pendiente', 'En curso', 'Finalizada', 'Cancelada'));

ALTER TABLE PuntoRuta
    ADD CONSTRAINT CK_PuntoRuta_TipoPunto
        CHECK (TipoPunto IN ('Origen', 'Destino'));

ALTER TABLE PuntoRuta
    ADD CONSTRAINT CK_PuntoRuta_Orden
        CHECK (Orden > 0);

ALTER TABLE ManipulacionCargamento
    ADD CONSTRAINT CK_ManipulacionCargamento_Tipo
        CHECK (TipoManipulacion IN ('Carga', 'Descarga', 'Estiba', 'Desestiba'));

ALTER TABLE RecorridoTransporte
    ADD CONSTRAINT CK_Recorrido_Fechas
        CHECK (FechaFin > FechaInicio);

ALTER TABLE RecorridoTransporte
    ADD CONSTRAINT CK_Recorrido_Kilometraje
        CHECK (KilometrajeFinal > KilometrajeInicial);

ALTER TABLE Asistente
    ADD CONSTRAINT CK_Asistente_Cargo
        CHECK (Cargo IN ('Ayudante de Carga'));
GO