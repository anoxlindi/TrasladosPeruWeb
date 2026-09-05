using Microsoft.Data.SqlClient;

namespace TrasladosPeruWeb.Data;

// Clase pequeña que abre una conexion nueva a SQL Server cada vez que se necesita.
// Lee la cadena de conexion desde appsettings.json (ConnectionStrings:TrasladosPeruDb).
public class ConexionFactory
{
    private readonly string _connectionString;

    public ConexionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("TrasladosPeruDb")
            ?? throw new InvalidOperationException("Falta la cadena de conexion 'TrasladosPeruDb' en appsettings.json");
    }

    public SqlConnection CrearConexion() => new SqlConnection(_connectionString);
}
