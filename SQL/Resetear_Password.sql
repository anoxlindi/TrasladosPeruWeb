-- Correr conectado a Azure (TrasladosPeru-RG)
-- Cambia el DNI de abajo por el de la persona que necesites resetear.

DECLARE @DniAResetear CHAR(9) = '42859179';

UPDATE Usuario
SET Password = RTRIM(@DniAResetear),
    FechaUltimoCambio = DATEADD(DAY, -8, GETDATE())  -- lo obliga a cambiarla al entrar
WHERE Dni = @DniAResetear;

-- Verificacion
SELECT u.Dni, e.Nombres, e.Apellidos, u.Password, u.FechaUltimoCambio
FROM Usuario u
JOIN Empleado e ON e.Dni = u.Dni
WHERE u.Dni = @DniAResetear;


------
SELECT u.Placa, cu.Toneladas, cu.CantidadPaletas
FROM UnidadTransporte u
JOIN CapacidadUnidad cu ON cu.CodigoCapacidad = u.CodigoCapacidad
ORDER BY cu.Toneladas DESC;