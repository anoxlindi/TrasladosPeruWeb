using System.Globalization;
using System.Text;

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

// Arma el texto que se le envia a cada trabajador y el link de WhatsApp.
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

    // paraChofer = true -> mensaje para el chofer (menciona al ayudante); false -> para el ayudante
    public static string Construir(RutaPlanificadaDto r, bool paraChofer)
    {
        var nombre = PrimerNombre(paraChofer ? r.Chofer : r.Ayudante);
        var sb = new StringBuilder();

        sb.AppendLine($"Hola {nombre} 👋");
        sb.AppendLine($"Esta es tu ruta del {Dias[(int)r.Fecha.DayOfWeek]} {r.Fecha:dd/MM/yyyy}:");
        sb.AppendLine();
        sb.AppendLine($"🏢 Cliente: {r.Cliente ?? "-"}");
        sb.AppendLine($"🕖 Salida de cochera: {Hora(r.HoraSalidaCochera)}");
        if (r.HoraCita.HasValue)
        {
            sb.AppendLine($"⏰ Hora de la cita: {Hora(r.HoraCita.Value)}");
        }
        sb.AppendLine($"🚚 Unidad: {r.Placa.Trim()}");
        sb.AppendLine($"🛣️ Ruta: {r.PuntoInicio} → {r.PuntoFin}");
        if (!string.IsNullOrWhiteSpace(r.Direccion))
        {
            sb.AppendLine($"📍 Dirección: {r.Direccion}");
        }

        if (paraChofer)
        {
            if (!string.IsNullOrWhiteSpace(r.Ayudante))
            {
                var tel = string.IsNullOrWhiteSpace(r.TelefonoAyudante) ? "" : $" ({r.TelefonoAyudante!.Trim()})";
                sb.AppendLine($"👷 Ayudante: {NombreBonito(r.Ayudante)}{tel}");
            }
            else
            {
                sb.AppendLine("👷 Ayudante: sin ayudante");
            }
        }
        else
        {
            var tel = string.IsNullOrWhiteSpace(r.TelefonoChofer) ? "" : $" ({r.TelefonoChofer!.Trim()})";
            sb.AppendLine($"🧑‍✈️ Chofer: {NombreBonito(r.Chofer)}{tel}");
        }

        if (!string.IsNullOrWhiteSpace(r.Observaciones))
        {
            sb.AppendLine($"📝 Nota: {r.Observaciones}");
        }

        sb.AppendLine();
        sb.Append("Cualquier duda avísame. ¡Buen viaje! 🙌");
        return sb.ToString();
    }

    // Link que abre WhatsApp con el mensaje ya escrito. Si no hay telefono valido devuelve null.
    public static string? LinkWhatsApp(string? telefono, string mensaje)
    {
        var digitos = new string((telefono ?? "").Where(char.IsDigit).ToArray());
        if (digitos.Length == 9) digitos = "51" + digitos;   // numero peruano sin codigo de pais
        if (digitos.Length < 11) return null;
        return $"https://wa.me/{digitos}?text={Uri.EscapeDataString(mensaje)}";
    }
}
