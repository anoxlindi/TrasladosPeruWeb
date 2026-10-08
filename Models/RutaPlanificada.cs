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
    public string? DniAyudante2 { get; set; }      // opcional - segundo ayudante (puede ser otro chofer)
    public bool EsDoblete { get; set; }            // el chofer hace 2 viajes en esta ruta
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
    public string? DniAyudante2 { get; set; }
    public string? Ayudante2 { get; set; }
    public string? TelefonoAyudante2 { get; set; }
    public bool EsDoblete { get; set; }
    public string? PuntoInicio { get; set; }
    public string? PuntoFin { get; set; }
    public string? Direccion { get; set; }
    public string? Observaciones { get; set; }
}

// El servidor de Azure trabaja en hora UTC: "hoy" se calcula con la hora de Peru (UTC-5, sin horario de verano).
public static class HoraPeru
{
    public static DateTime Hoy => DateTime.UtcNow.AddHours(-5).Date;
}

// Datos de cada persona (chofer o ayudante) dentro de la tarjeta de una ruta.
public class PersonaRutaVm
{
    public long CodigoPlan { get; set; }
    public string Destino { get; set; } = "chofer";   // "chofer", "ayudante1" o "ayudante2"
    public string Etiqueta { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Link { get; set; }                  // link directo a WhatsApp (null = sin telefono)
    public string Mensaje { get; set; } = "";
    public bool Automatico { get; set; }
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

    // Nombre de la persona segun el destino del mensaje: "chofer", "ayudante1" o "ayudante2".
    private static string? NombreDe(RutaPlanificadaDto r, string destino) => destino switch
    {
        "chofer" => r.Chofer,
        "ayudante1" => r.Ayudante,
        _ => r.Ayudante2
    };

    // El resto de la tripulacion de la ruta, sin incluir a quien recibe el mensaje
    // (el chofer ve a los 2 ayudantes; cada ayudante ve al chofer y al otro ayudante, si hay).
    private static List<string> Companeros(RutaPlanificadaDto r, string destino)
    {
        var lista = new List<string>();
        if (destino != "chofer" && !string.IsNullOrWhiteSpace(r.Chofer))
        {
            lista.Add($"Chofer: {NombreBonito(r.Chofer)}{Tel(r.TelefonoChofer)}");
        }
        if (destino != "ayudante1" && !string.IsNullOrWhiteSpace(r.Ayudante))
        {
            lista.Add($"Ayudante: {NombreBonito(r.Ayudante)}{Tel(r.TelefonoAyudante)}");
        }
        if (destino != "ayudante2" && !string.IsNullOrWhiteSpace(r.Ayudante2))
        {
            lista.Add($"Ayudante: {NombreBonito(r.Ayudante2)}{Tel(r.TelefonoAyudante2)}");
        }
        if (lista.Count == 0)
        {
            lista.Add("Ayudante: sin ayudante");
        }
        return lista;
    }

    // Emojis escritos como codigos (\U....) para que no se danen si el archivo se guarda con otra codificacion.
    private const string EmHola = "\U0001F44B";      // saludo
    private const string EmFecha = "\U0001F4C5";     // calendario
    private const string EmDoblete = "\U0001F501";   // doble flecha (viaje doble)
    private const string EmCliente = "\U0001F3E2";   // edificio
    private const string EmSalida = "⏰";        // reloj despertador
    private const string EmCita = "\U0001F91D";      // apreton de manos
    private const string EmUnidad = "\U0001F69A";    // camion
    private const string EmRuta = "\U0001F4CD";      // chincheta
    private const string EmDireccion = "\U0001F4CC"; // pin
    private const string EmPersona = "\U0001F464";   // persona
    private const string EmNota = "\U0001F4DD";      // nota
    private const string EmCierre = "\U0001F64C";    // manos arriba

    // destino = "chofer", "ayudante1" o "ayudante2": a quien va dirigido el mensaje.
    // Usa emojis y las negritas de WhatsApp (*texto*).
    public static string Construir(RutaPlanificadaDto r, string destino)
    {
        var nombre = PrimerNombre(NombreDe(r, destino));
        var sb = new StringBuilder();

        sb.AppendLine($"{EmHola} Hola {nombre},");
        sb.AppendLine($"{EmFecha} Esta es tu ruta del {FechaLarga(r.Fecha)}:");
        if (r.EsDoblete)
        {
            sb.AppendLine($"{EmDoblete} *Ojo: esta ruta es DOBLETE* (2 viajes).");
        }
        sb.AppendLine();
        sb.AppendLine($"{EmCliente} *Cliente:* {r.Cliente ?? "-"}");
        sb.AppendLine($"{EmSalida} *Salida de cochera:* {Hora(r.HoraSalidaCochera)}");
        if (r.HoraCita.HasValue)
        {
            sb.AppendLine($"{EmCita} *Hora de la cita:* {Hora(r.HoraCita.Value)}");
        }
        sb.AppendLine($"{EmUnidad} *Unidad:* {r.Placa.Trim()}");
        sb.AppendLine($"{EmRuta} *Ruta:* {r.PuntoInicio} → {r.PuntoFin}");
        if (!string.IsNullOrWhiteSpace(r.Direccion))
        {
            sb.AppendLine($"{EmDireccion} *Dirección:* {r.Direccion}");
        }

        foreach (var companero in Companeros(r, destino))
        {
            var separador = companero.IndexOf(':');
            sb.AppendLine($"{EmPersona} *{companero[..separador]}:*{companero[(separador + 1)..]}");
        }

        if (!string.IsNullOrWhiteSpace(r.Observaciones))
        {
            sb.AppendLine($"{EmNota} *Nota:* {r.Observaciones}");
        }

        sb.AppendLine();
        sb.Append($"{EmCierre} Cualquier duda avísame. ¡Buen viaje!");
        return sb.ToString();
    }

    // Parametros {{1}} a {{10}} de la plantilla "ruta_asignada" de WhatsApp Business.
    // Meta no admite saltos de linea ni valores vacios dentro de los parametros.
    public static List<string> Parametros(RutaPlanificadaDto r, string destino)
    {
        var nombre = PrimerNombre(NombreDe(r, destino));
        var ruta = r.EsDoblete ? $"{r.PuntoInicio} → {r.PuntoFin} (DOBLETE)" : $"{r.PuntoInicio} → {r.PuntoFin}";
        return new List<string>
        {
            Limpiar(nombre),                                                       // {{1}} nombre
            Limpiar(FechaLarga(r.Fecha)),                                          // {{2}} fecha
            Limpiar(r.Cliente),                                                    // {{3}} cliente
            Limpiar(Hora(r.HoraSalidaCochera)),                                    // {{4}} salida de cochera
            Limpiar(r.HoraCita.HasValue ? Hora(r.HoraCita.Value) : "-"),           // {{5}} hora de la cita
            Limpiar(r.Placa),                                                      // {{6}} unidad
            Limpiar(ruta),                                                         // {{7}} ruta
            Limpiar(r.Direccion),                                                  // {{8}} direccion
            Limpiar(string.Join(" / ", Companeros(r, destino))),                   // {{9}} companeros
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
