USE TrasladosPeru2;  -- o TrasladosPeru-RG si lo corres en Azure
GO

-- 1) Verificar si tiene viajes registrados como ayudante (por si hay que revisar antes de mover)
SELECT * FROM RecorridoTransporte WHERE RTRIM(DniAyudante) = '10625256';
GO

-- 2) Mover de Asistente a Conductor
DELETE FROM Asistente WHERE Dni = '10625256';

IF NOT EXISTS (SELECT 1 FROM Conductor WHERE Dni = '10625256')
    INSERT INTO Conductor (Dni) VALUES ('10625256');
GO

-- 3) Verificacion final
SELECT e.Dni, e.Nombres, e.Apellidos,
    CASE WHEN c.Dni IS NOT NULL THEN 'Chofer' WHEN a.Dni IS NOT NULL THEN 'Ayudante' END AS Rol
FROM Empleado e
LEFT JOIN Conductor c ON c.Dni = e.Dni
LEFT JOIN Asistente a ON a.Dni = e.Dni
WHERE e.Dni = '10625256';

SELECT e.Dni, e.Nombres, e.Apellidos, e.Estado
FROM Empleado e
JOIN Conductor c ON c.Dni = e.Dni
ORDER BY e.Nombres;