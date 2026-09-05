namespace TrasladosPeruWeb.Models;

public class Usuario
{
    public string Dni { get; set; } = "";
    public string Password { get; set; } = "";
    public DateTime FechaUltimoCambio { get; set; }
    public bool Activo { get; set; }
    public bool EsAdministrador { get; set; }
    public string Nombres { get; set; } = "";
}

public class LoginForm
{
    public string Dni { get; set; } = "";
    public string Password { get; set; } = "";
}

public class CambiarPasswordForm
{
    public string Dni { get; set; } = "";
    public string PasswordActual { get; set; } = "";
    public string PasswordNueva { get; set; } = "";
    public string PasswordNuevaConfirmar { get; set; } = "";
}
