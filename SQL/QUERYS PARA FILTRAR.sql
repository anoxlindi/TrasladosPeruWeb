-- 1. Todo, lo más reciente primero
SELECT * FROM vw_ReporteRecorridos ORDER BY FechaInicio DESC;

-- 2. Solo una unidad (camión) especifica
SELECT * FROM vw_ReporteRecorridos WHERE Placa = 'BRX 826';

-- 3. Solo un chofer
SELECT * FROM vw_ReporteRecorridos WHERE Chofer LIKE '%AGUERO%';

-- 4. Viajes de un dia especifico
SELECT * FROM vw_ReporteRecorridos WHERE CAST(FechaInicio AS DATE) = '2026-08-30';

-- 5. Viajes de HOY
SELECT * FROM vw_ReporteRecorridos WHERE CAST(FechaInicio AS DATE) = CAST(GETDATE() AS DATE);

-- 6. Un camion + un cliente especifico
SELECT * FROM vw_ReporteRecorridos
WHERE Placa = 'CJT-708' AND Cliente = 'SOCIEDAD QUIMICA ALEMANA S.A.';

-- 7. Combinando: un chofer, en un rango de fechas
SELECT * FROM vw_ReporteRecorridos
WHERE Chofer LIKE '%CHUMBEZ%'
  AND FechaInicio BETWEEN '2026-08-01' AND '2026-08-31';

-- 8. Viajes SIN ayudante (chofer solo)
SELECT * FROM vw_ReporteRecorridos WHERE Ayudante IS NULL;

-- 9. Viajes a los que Gina aun no les puso costo
SELECT * FROM vw_ReporteRecorridos WHERE Costo IS NULL;

------------------------------------
-- Km totales recorridos por cada unidad
SELECT Placa, SUM(KilometrajeRecorrido) AS KmTotales, COUNT(*) AS CantViajes
FROM vw_ReporteRecorridos
GROUP BY Placa
ORDER BY KmTotales DESC;
---------------------------------------
USE TrasladosPeru2;
SELECT * FROM vw_ReporteRecorridos;
-- Cuantos viajes hizo cada chofer
SELECT Chofer, COUNT(*) AS CantViajes
FROM vw_ReporteRecorridos
GROUP BY Chofer
ORDER BY CantViajes DESC;

-- Costo total facturado por cliente
SELECT Cliente, SUM(Costo + ISNULL(CostoAdicional,0)) AS TotalCobrado
FROM vw_ReporteRecorridos
WHERE Cliente IS NOT NULL
GROUP BY Cliente
ORDER BY TotalCobrado DESC;