-- 1) Placa corregida y camiones eliminados
SELECT Placa FROM UnidadTransporte WHERE Placa IN ('CNG-873', 'CNH-873', 'DOM 807', 'BKM 840');
-- esperado: solo aparece CNH-873, nada más

-- 2) Tarifas asignadas
SELECT Placa, TarifaPeaje FROM UnidadTransporte ORDER BY Placa;

-- 3) Columnas nuevas existen
SELECT TOP 5 CodigoRecorrido, KilometrajeInicioCochera, KilometrajeFinalCochera,
       FuePeaje, CantidadPeajes, FueLineaAmarilla, CantidadLineaAmarilla
FROM RecorridoTransporte;

-- 4) Vista actualizada funciona
SELECT TOP 5 * FROM vw_ReporteRecorridos;