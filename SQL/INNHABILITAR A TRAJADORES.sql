-- Correr conectado a Azure (TrasladosPeru-RG)

-- 1) Le quita el acceso al sistema (ya no puede iniciar sesion)
UPDATE Usuario SET Activo = 0 WHERE Dni = '74374197';

-- 2) Lo marca como inactivo en la planilla de empleados
UPDATE Empleado SET Estado = 'Inactivo' WHERE Dni = '74374197';

-- Verificacion
SELECT e.Dni, e.Nombres, e.Apellidos, e.Estado, u.Activo AS AccesoActivo
FROM Empleado e
JOIN Usuario u ON u.Dni = e.Dni
WHERE e.Dni = '74374197';

--ver activos o innactivos

SELECT Dni, Nombres, Apellidos FROM Empleado WHERE Estado = 'Inactivo';

SELECT Dni, Nombres, Apellidos FROM Empleado WHERE Estado = 'Activo';