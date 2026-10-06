using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TrasladosPeruWeb.Models;

namespace TrasladosPeruWeb.Services;

// Envia el mensaje de la ruta por la API oficial de WhatsApp Business (Cloud API de Meta).
//
// Se configura con variables de entorno en Azure (App Service > Variables de entorno):
//   WhatsApp__Token            -> token de acceso permanente de Meta
//   WhatsApp__PhoneNumberId    -> "Identificador del numero de telefono" de WhatsApp Business
//   WhatsApp__PlantillaNombre  -> nombre de la plantilla aprobada (por defecto: ruta_asignada)
//   WhatsApp__Idioma           -> codigo de idioma de la plantilla (por defecto: es)
//
// Si falta Token o PhoneNumberId, EstaConfigurado es false y la pagina sigue usando el link normal de WhatsApp.
public class WhatsAppService
{
    private readonly HttpClient _http;
    private readonly string? _token;
    private readonly string? _phoneNumberId;
    private readonly string _plantilla;
    private readonly string _idioma;

    public WhatsAppService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _token = config["WhatsApp:Token"];
        _phoneNumberId = config["WhatsApp:PhoneNumberId"];
        _plantilla = string.IsNullOrWhiteSpace(config["WhatsApp:PlantillaNombre"]) ? "ruta_asignada" : config["WhatsApp:PlantillaNombre"]!;
        _idioma = string.IsNullOrWhiteSpace(config["WhatsApp:Idioma"]) ? "es" : config["WhatsApp:Idioma"]!;
    }

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(_token) && !string.IsNullOrWhiteSpace(_phoneNumberId);

    // Devuelve (true, "") si Meta acepto el mensaje, o (false, motivo) si fallo.
    public async Task<(bool Ok, string Detalle)> EnviarRutaAsync(string? telefono, List<string> parametros)
    {
        if (!EstaConfigurado)
        {
            return (false, "El envío automático todavía no está configurado.");
        }

        var numero = MensajesRuta.TelefonoInternacional(telefono);
        if (numero is null)
        {
            return (false, "Esa persona no tiene un teléfono válido registrado.");
        }

        var cuerpo = new
        {
            messaging_product = "whatsapp",
            to = numero,
            type = "template",
            template = new
            {
                name = _plantilla,
                language = new { code = _idioma },
                components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = parametros.Select(p => new { type = "text", text = p }).ToArray()
                    }
                }
            }
        };

        try
        {
            using var peticion = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://graph.facebook.com/v21.0/{_phoneNumberId}/messages")
            {
                Content = JsonContent.Create(cuerpo)
            };
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            using var respuesta = await _http.SendAsync(peticion);
            var texto = await respuesta.Content.ReadAsStringAsync();

            if (respuesta.IsSuccessStatusCode)
            {
                return (true, "");
            }
            return (false, ExtraerError(texto));
        }
        catch (Exception ex)
        {
            return (false, "No se pudo conectar con WhatsApp: " + ex.Message);
        }
    }

    // Meta responde {"error":{"message":"...","code":...}}; se muestra el mensaje tal cual para poder diagnosticar
    private static string ExtraerError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var mensaje))
            {
                return mensaje.GetString() ?? "Error desconocido de WhatsApp.";
            }
        }
        catch (JsonException)
        {
            // la respuesta no era JSON; se devuelve un texto generico
        }
        return "WhatsApp rechazó el mensaje.";
    }
}
