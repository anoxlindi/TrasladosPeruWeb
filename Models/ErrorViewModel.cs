namespace TrasladosPeruWeb.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    // Para ver que fallo y donde, sin tener que revisar los logs de Azure.
    public string? RutaOriginal { get; set; }
    public string? Mensaje { get; set; }

    // A donde manda el boton de "Volver": el modulo donde paso el error
    // (Recorridos, Planificar rutas, etc.), no siempre el mismo para todos.
    public string VolverUrl => (RutaOriginal ?? "").StartsWith("/Planificacion") ? "/Planificacion"
        : (RutaOriginal ?? "").StartsWith("/Recorrido") ? "/Recorrido"
        : "/";

    public string VolverTexto => (RutaOriginal ?? "").StartsWith("/Planificacion") ? "Volver a Planificar rutas"
        : (RutaOriginal ?? "").StartsWith("/Recorrido") ? "Volver a Recorridos"
        : "Volver al inicio";
}
