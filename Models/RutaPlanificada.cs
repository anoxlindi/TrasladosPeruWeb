using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TrasladosPeruWeb.Models;

// Datos del formulario "Planificar ruta".
public class RutaPlanificadaForm
{
    public long? CodigoPlan { get; set; }          // con valor = edicion, null = ruta nueva
    public DateTime Fecha { get; set; }
    public TimeSpan? HoraSalidaCochera { get; set; }
    public TimeSpan? HoraCita { get; set; }        // opcional
    public string? Ruc { get; set; }               // cliente del catalogo (vacio si usa ClienteOtro)
    public string? ClienteOtro { get; set; }
    public long CodigoUnidad { get; set; }
    public string DniChofer { get; set; } = "";
    public string? DniAyudante { get; set; }       // opcional
    public string PuntoInicio { get; set; } = "";
    public string PuntoFin { get; set; } = "";
    public string? Direccion { get; set; }         // direccion exacta de la cita
    public string? Observaciones { get; set; }

    // Filtro que estaba aplicado en la lista: viaja oculto en Editar para volver con el mismo filtro
    public DateTime? FiltroFechaDesde { get; set; }
    public DateTime? FiltroFechaHasta { get; set; }
}

// Ruta ya armada, con nombres y telefonos listos para mostrar y para el mensaje.
public class RutaPlanificadaDto
{
    public long CodigoPlan { get; set; }
    public DateTime Fecha { get; set; }
    public TimeSpan HoraSalidaCochera { get; set; }
    public TimeSpan? HoraCita { get; set; }
    public string? Cliente { get; set; }
    public string Placa { get; set; } = "";
    public string? DniChofer { get; set; }
    public string? Chofer { get; set; }
    public string? TelefonoChofer { get; set; }
    public string? DniAyudante { get; set; }
    public string? Ayudante { get; set; }
    public string? TelefonoAyudante { get; set; }
    public string? PuntoInicio { get; set; }
    public string? PuntoFin { get; set; }
    public string? Direccion { get; set; }
    public string? Observaciones { get; set; }
}

public class FiltroPlanificacion
{
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}

// Arma el texto que se le envia a cada trabajador (copiar / link de WhatsApp)
// y los parametros de la plantilla para el envio automatico por la API.
public static class MensajesRuta
{
    private static readonly string[] Dias =
        { "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado" };

    private static string PrimerNombre(string? nombreCompleto)
    {
        var texto = (nombreCompleto ?? "").Trim();
        if (texto.Length == 0) return "";
        var primero = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(primero.ToLowerInvariant());
    }

    private static string NombreBonito(string? nombreCompleto)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase((nombreCompleto ?? "").Trim().ToLowerInvariant());
    }

    private static string Hora(TimeSpan hora) => $"{hora.Hours:00}:{hora.Minutes:00}";

    private static string FechaLarga(DateTime fecha) => $"{Dias[(int)fecha.DayOfWeek]} {fecha:dd/MM/yyyy}";

    private static string Tel(string? telefono) =>
        string.IsNullOrWhiteSpace(telefono) ? "" : $" ({telefono.Trim()})";

    // Texto de la linea del companero: el chofer ve a su ayudante y el ayudante ve a su chofer
    private static string Companero(RutaPlanificadaDto r, bool paraChofer)
    {
        if (paraChofer)
        {
            return string.IsNullOrWhiteSpace(r.Ayudante)
                ? "Ayudante: sin ayudante"
                : $"Ayudante: {NombreBonito(r.Ayudante)}{Tel(r.TelefonoAyudante)}";
        }
        return $"Chofer: {NombreBonito(r.Chofer)}{Tel(r.TelefonoChofer)}";
    }

    // paraChofer = true -> mensaje para el chofer (menciona al ayudante); false -> para el ayudante.
    // Sin emojis a proposito (se veian como "?" en algunos navegadores); usa las negritas de WhatsApp (*texto*).
    public static string Construir(RutaPlanificadaDto r, bool paraChofer)
    {
        var nombre = PrimerNombre(paraChofer ? r.Chofer : r.Ayudante);
        var sb = new StringBuilder();

        sb.AppendLine($"Hola {nombre},");
        sb.AppendLine($"Esta es tu ruta del {FechaLarga(r.Fecha)}:");
        sb.AppendLine();
        sb.AppendLine($"*Cliente:* {r.Cliente ?? "-"}");
        sb.AppendLine($"*Salida de cochera:* {Hora(r.HoraSalidaCochera)}");
        if (r.HoraCita.HasValue)
        {
            sb.AppendLine($"*Hora de la cita:* {Hora(r.HoraCita.Value)}");
        }
        sb.AppendLine($"*Unidad:* {r.Placa.Trim()}");
        sb.AppendLine($"*Ruta:* {r.PuntoInicio} → {r.PuntoFin}");
        if (!string.IsNullOrWhiteSpace(r.Direccion))
        {
            sb.AppendLine($"*Dirección:* {r.Direccion}");
        }

        var companero = Companero(r, paraChofer);
        var separador = companero.IndexOf(':');
        sb.AppendLine($"*{companero[..separador]}:*{companero[(separador + 1)..]}");

        if (!string.IsNullOrWhiteSpace(r.Observaciones))
        {
            sb.AppendLine($"*Nota:* {r.Observaciones}");
        }

        sb.AppendLine();
        sb.Append("Cualquier duda avísame. ¡Buen viaje!");
        return sb.ToString();
    }

    // Parametros {{1}} a {{10}} de la plantilla "ruta_asignada" de WhatsApp Business.
    // Meta no admite saltos de linea ni valores vacios dentro de los parametros.
    public static List<string> Parametros(RutaPlanificadaDto r, bool paraChofer)
    {
        var nombre = PrimerNombre(paraChofer ? r.Chofer : r.Ayudante);
        return new List<string>
        {
            Limpiar(nombre),                                                       // {{1}} nombre
            Limpiar(FechaLarga(r.Fecha)),                                          // {{2}} fecha
            Limpiar(r.Cliente),                                                    // {{3}} cliente
            Limpiar(Hora(r.HoraSalidaCochera)),                                    // {{4}} salida de cochera
            Limpiar(r.HoraCita.HasValue ? Hora(r.HoraCita.Value) : "-"),           // {{5}} hora de la cita
            Limpiar(r.Placa),                                                      // {{6}} unidad
            Limpiar($"{r.PuntoInicio} → {r.PuntoFin}"),                            // {{7}} ruta
            Limpiar(r.Direccion),                                                  // {{8}} direccion
            Limpiar(Companero(r, paraChofer)),                                     // {{9}} companero
            Limpiar(r.Observaciones)                                               // {{10}} nota
        };
    }

    // Quita saltos de linea/tabs, reduce espacios repetidos y nunca devuelve vacio
    private static string Limpiar(string? texto)
    {
        var limpio = Regex.Replace(texto ?? "", @"\s+", " ").Trim();
        if (limpio.Length == 0) return "-";
        return limpio.Length > 500 ? limpio[..500] : limpio;
    }

    // Telefono en formato internacional sin "+" (ej: 51944973091). Devuelve null si no es valido.
    public static string? TelefonoInternacional(string? telefono)
    {
        var digitos = new string((telefono ?? "").Where(char.IsDigit).ToArray());
        if (digitos.Length == 9) digitos = "51" + digitos;   // numero peruano sin codigo de pais
        return digitos.Length >= 11 ? digitos : null;
    }

    // Link que abre DIRECTO la app de WhatsApp (escritorio) con el chat y el mensaje ya escritos,
    // sin pasar por la pagina web de WhatsApp. Solo falta apretar Enter.
    // (respaldo cuando no esta configurado el envio automatico)
    public static string? LinkWhatsApp(string? telefono, string mensaje)
    {
        var numero = TelefonoInternacional(telefono);
        if (numero is null) return null;
        return $"whatsapp://send?phone={numero}&text={Uri.EscapeDataString(mensaje)}";
    }
}
