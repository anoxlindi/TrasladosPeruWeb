namespace TrasladosPeruWeb.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    // Para ver que fallo y donde, sin tener que revisar los logs de Azure.
    public string? RutaOriginal { get; set; }
    public string? Mensaje { get; set; }
}
