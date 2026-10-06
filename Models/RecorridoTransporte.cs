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
    public long? CodigoRecorrido { get; set; } // si tiene valor, es una edicion; si es null, es un viaje nuevo
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

    // Kilometraje de cochera: el recorrido completo desde que sale de cochera hasta que vuelve,
    // ademas del kilometraje "del servicio" (KilometrajeInicial/Final) que ya existia.
    public int? KilometrajeInicioCochera { get; set; }
    public int? KilometrajeFinalCochera { get; set; }

    // Peaje: si paso por peaje, cuantos peajes, y el costo se calcula solo (tarifa por placa)
    public bool FuePeaje { get; set; }
    public int CantidadPeajes { get; set; }

    // Linea amarilla: mismo patron que peaje (Si/No + cantidad 1-4), tarifa por placa
    public bool FueLineaAmarilla { get; set; }
    public int CantidadLineaAmarilla { get; set; }

    // Estos 4 campos solo viajan "de ida" desde el Index filtrado hacia Editar, y "de vuelta"
    // en un campo oculto del formulario de Editar, para que al Guardar, el redirect a Index
    // reaplique el mismo filtro que la administradora tenia puesto (no se resetea el filtro).
    public DateTime? FiltroFechaDesde { get; set; }
    public DateTime? FiltroFechaHasta { get; set; }
    public string? FiltroDniChofer { get; set; }
    public long? FiltroCodigoUnidad { get; set; }
}

// Formulario de "Olvidé mi contraseña": solo pide el DNI, verifica que exista
// y resetea la contraseña a su propio DNI, forzando el cambio en el siguiente login
// (automatiza lo que antes se hacia a mano con un script SQL).
public class OlvidePasswordForm
{
    public string Dni { get; set; } = "";
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
    public int? KilometrajeInicioCochera { get; set; }
    public int? KilometrajeFinalCochera { get; set; }
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
    public bool FuePeaje { get; set; }
    public int CantidadPeajes { get; set; }
    public decimal CostoPeajes { get; set; }
    public bool FueLineaAmarilla { get; set; }
    public int CantidadLineaAmarilla { get; set; }
    public decimal CostoLineaAmarilla { get; set; }
}

// Filtros que arma Gina (administradora) en la pantalla de Recorridos.
// Cambio: ya no es una sola Fecha, ahora es un RANGO Desde/Hasta, para poder
// filtrar varios dias y que el Excel exportado respete ese mismo rango.
public class FiltroRecorridos
{
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public string? DniChofer { get; set; }
    public long? CodigoUnidad { get; set; }
}

// Para llenar los <select> del formulario "Nuevo recorrido"
public class OpcionSelect
{
    public string Codigo { get; set; } = "";
    public string Texto { get; set; } = "";
}

// Distritos de Lima Metropolitana y Lima Provincia, para la barra desplegable
// de Punto de inicio / Punto final (antes eran campos de texto libre).
public static class Distritos
{
    public static readonly string[] Lista = new[]
    {
        // Lima Metropolitana
        "Ancón", "Ate", "Barranco", "Breña", "Carabayllo", "Chaclacayo", "Chorrillos",
        "Cieneguilla", "Comas", "El Agustino", "Independencia", "Jesús María", "La Molina",
        "La Victoria", "Lima (Cercado)", "Lince", "Los Olivos", "Lurigancho (Chosica)",
        "Lurín", "Magdalena del Mar", "Miraflores", "Pachacámac", "Pucusana",
        "Pueblo Libre", "Puente Piedra", "Punta Hermosa", "Punta Negra", "Rímac",
        "San Bartolo", "San Borja", "San Isidro", "San Juan de Lurigancho",
        "San Juan de Miraflores", "San Luis", "San Martín de Porres", "San Miguel",
        "Santa Anita", "Santa María del Mar", "Santa Rosa", "Santiago de Surco",
        "Surquillo", "Villa El Salvador", "Villa María del Triunfo",
        // Lima Provincia
        "Barranca", "Canta", "Cañete", "Huaral", "Huarochirí", "Huaura", "Oyón",
        "Yauyos"
    };
}
