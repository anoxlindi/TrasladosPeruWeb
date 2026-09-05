namespace TrasladosPeruWeb.Models;

// Refleja exactamente la tabla RecorridoTransporte
public class RecorridoTransporte
{
    public long CodigoRecorrido { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int KilometrajeInicial { get; set; }
    public int KilometrajeFinal { get; set; }
    public int KilometrajeRecorrido { get; set; } // columna calculada en SQL, no se inserta
    public long CodigoRuta { get; set; }
    public long CodTransporteCargamento { get; set; }
    public string? DniChofer { get; set; }
    public string? DniAyudante { get; set; }
}

// Datos del formulario "Nuevo viaje": todo lo que la persona elige en pantalla.
// El controlador arma Cargamento, TransporteCargamento, Ruta y Solicitud por detras.
public class NuevoViajeForm
{
    public string? Ruc { get; set; }              // cliente del catalogo (vacio si usa ClienteOtro)
    public string? ClienteOtro { get; set; }       // texto libre si el cliente no esta en la lista
    public string DniChofer { get; set; } = "";
    public string? DniAyudante { get; set; }       // opcional: el chofer puede ir solo
    public long CodigoUnidad { get; set; }
    public string TipoCargamento { get; set; } = "";
    public decimal Peso { get; set; }
    public decimal? Costo { get; set; }
    public decimal? CostoAdicional { get; set; }
    public string PuntoInicio { get; set; } = "";
    public string PuntoFin { get; set; } = "";
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int KilometrajeInicial { get; set; }
    public int KilometrajeFinal { get; set; }
}

// Version "bonita" para mostrar en la vista: ya trae el nombre del cliente y la placa,
// no solo los codigos. Viene de la vista SQL vw_ReporteRecorridos.
public class ReporteRecorridoDto
{
    public long CodigoRecorrido { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int KilometrajeInicial { get; set; }
    public int KilometrajeFinal { get; set; }
    public int KilometrajeRecorrido { get; set; }
    public string Placa { get; set; } = "";
    public long CodigoUnidad { get; set; }
    public string? DniChofer { get; set; }
    public string? DniAyudante { get; set; }
    public string? Cliente { get; set; }
    public string? Chofer { get; set; }
    public string? Ayudante { get; set; }
    public string? PuntoInicio { get; set; }
    public string? PuntoFin { get; set; }
    public decimal? Costo { get; set; }
    public decimal? CostoAdicional { get; set; }
}

// Filtros que arma Gina (administradora) en la pantalla de Recorridos
public class FiltroRecorridos
{
    public DateTime? Fecha { get; set; }
    public string? DniChofer { get; set; }
    public long? CodigoUnidad { get; set; }
}

// Para llenar los <select> del formulario "Nuevo recorrido"
public class OpcionSelect
{
    public string Codigo { get; set; } = "";
    public string Texto { get; set; } = "";
}
