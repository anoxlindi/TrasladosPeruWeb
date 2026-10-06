using Dapper;
using TrasladosPeruWeb.Data;
using TrasladosPeruWeb.Models;

namespace TrasladosPeruWeb.Repositories;

public class UsuarioRepository
{
    private readonly ConexionFactory _conexion;

    public UsuarioRepository(ConexionFactory conexion)
    {
        _conexion = conexion;
    }

    // Busca el usuario por Dni y contraseña. Devuelve null si no coincide o esta inactivo.
    public async Task<Usuario?> ValidarCredencialesAsync(string dni, string password)
    {
        using var db = _conexion.CrearConexion();
        var sql = @"SELECT u.Dni, u.Password, u.FechaUltimoCambio, u.Activo, u.EsAdministrador, e.Nombres
                    FROM Usuario u
                    JOIN Empleado e ON e.Dni = u.Dni
                    WHERE u.Dni = @dni AND u.Password = @password AND u.Activo = 1";
        return await db.QueryFirstOrDefaultAsync<Usuario>(sql, new { dni, password });
    }

    // True si ya pasaron 7 dias o mas desde el ultimo cambio de contraseña
    public bool DebeCambiarPassword(Usuario u) =>
        (DateTime.Now - u.FechaUltimoCambio).TotalDays >= 7;

    public async Task<bool> CambiarPasswordAsync(string dni, string passwordActual, string passwordNueva)
    {
        using var db = _conexion.CrearConexion();
        var sql = @"UPDATE Usuario
                    SET Password = @passwordNueva, FechaUltimoCambio = GETDATE()
                    WHERE Dni = @dni AND Password = @passwordActual AND Activo = 1";
        var filas = await db.ExecuteAsync(sql, new { dni, passwordActual, passwordNueva });
        return filas > 0;
    }

    // Para cuando la persona deja la empresa: le quita el acceso sin borrar su historial
    public async Task DesactivarAsync(string dni)
    {
        using var db = _conexion.CrearConexion();
        await db.ExecuteAsync("UPDATE Usuario SET Activo = 0 WHERE Dni = @dni", new { dni });
    }

    // True si el DNI existe como usuario activo (para la pantalla "Olvidé mi contraseña")
    public async Task<bool> ExisteUsuarioActivoAsync(string dni)
    {
        using var db = _conexion.CrearConexion();
        var cantidad = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Usuario WHERE RTRIM(Dni) = @dni AND Activo = 1", new { dni });
        return cantidad > 0;
    }

    // "Olvidé mi contraseña": automatiza lo que antes se hacia a mano con el script
    // Resetear_Password.sql -- resetea la contraseña al propio DNI y la "backdatea"
    // 8 dias para que el sistema obligue a cambiarla en el siguiente login (igual que
    // la regla de los 7 dias que ya existia).
    public async Task<bool> ResetearPasswordAsync(string dni)
    {
        using var db = _conexion.CrearConexion();
        var sql = @"UPDATE Usuario
                    SET Password = RTRIM(Dni), FechaUltimoCambio = DATEADD(DAY, -8, GETDATE())
                    WHERE RTRIM(Dni) = @dni AND Activo = 1";
        var filas = await db.ExecuteAsync(sql, new { dni });
        return filas > 0;
    }
}
