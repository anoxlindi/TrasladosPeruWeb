SELECT e.Dni, e.Nombres, e.Apellidos,
    CASE
        WHEN c.Dni IS NOT NULL THEN 'Chofer'
        WHEN a.Dni IS NOT NULL THEN 'Ayudante'
        ELSE 'Sin rol asignado'
    END AS Rol
FROM Empleado e
LEFT JOIN Conductor c ON c.Dni = e.Dni
LEFT JOIN Asistente a ON a.Dni = e.Dni WHERE Estado = 'Activo'


WHERE e.Dni = '42859179'
ORDER BY e.Dni;