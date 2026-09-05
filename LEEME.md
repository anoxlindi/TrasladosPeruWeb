# TrasladosPerú - Panel web

## Qué es esto
Un proyecto ASP.NET Core MVC (C#) que se conecta a tu base de datos SQL Server (`TrasladosPeru2`)
y permite ver, registrar y eliminar recorridos, además de exportar todo a Excel con una hoja de métricas.

## Requisitos (una sola vez)
1. Instalar el **.NET SDK 8** (gratis): https://dotnet.microsoft.com/download
2. Tener SQL Server corriendo con la base `TrasladosPeru2` ya creada (el script que ya armamos).
3. Correr el archivo `SQL/vw_ReporteRecorridos.sql` en tu SQL Server (crea la vista que usa la página).

## Pasos para correrlo
1. Abre la carpeta `TrasladosPeruWeb` en VS Code.
2. Revisa `appsettings.json` y ajusta la línea de conexión si tu SQL Server no es local
   o si usas usuario/contraseña en vez de autenticación de Windows:
   ```
   "Server=localhost;Database=TrasladosPeru2;Trusted_Connection=True;TrustServerCertificate=True;"
   ```
   Si usas login de SQL Server (no Windows), cámbiala por algo como:
   ```
   "Server=localhost;Database=TrasladosPeru2;User Id=sa;Password=TU_CLAVE;TrustServerCertificate=True;"
   ```
3. Abre una terminal en esa carpeta y corre:
   ```
   dotnet restore
   dotnet run
   ```
4. Te va a mostrar algo como `Now listening on: http://localhost:5000`. Abre esa dirección en tu navegador.
5. Click en "Recorridos" en el menú de arriba. Ahí puedes:
   - Ver la lista de viajes (con placa, cliente y km recorridos ya calculados)
   - Click en "+ Nuevo recorrido" para registrar uno
   - Click en "Exportar a Excel" para bajar el archivo con el detalle y las métricas por cliente

## Cómo está organizado (patrón MVC)
- **Models/**: las "formas" de los datos (qué campos tiene un Recorrido)
- **Data/**: la conexión a SQL Server
- **Repositories/**: las consultas SQL reales (INSERT, SELECT, DELETE)
- **Controllers/**: reciben el clic del usuario y deciden qué hacer (llamar al repositorio, mostrar una vista)
- **Views/**: el HTML que ve la persona
- **wwwroot/css/**: los estilos

## Para agregar las demás tablas (Cliente, Solicitud, UnidadTransporte, etc.)
Sigue el mismo patrón de `RecorridoTransporte`: un modelo en `Models/`, un repositorio en
`Repositories/`, un controlador en `Controllers/` y sus vistas en `Views/<NombreTabla>/`.
Si quieres, en el siguiente paso armamos juntos el de `Cliente` o `UnidadTransporte`.
