using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrasladosPeruWeb.Models;
using TrasladosPeruWeb.Repositories;

namespace TrasladosPeruWeb.Controllers;

public class CuentaController : Controller
{
    private readonly UsuarioRepository _usuarios;

    public CuentaController(UsuarioRepository usuarios)
    {
        _usuarios = usuarios;
    }

    [AllowAnonymous]
    public IActionResult Login() => View(new LoginForm());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginForm form)
    {
        var usuario = await _usuarios.ValidarCredencialesAsync(form.Dni.Trim(), form.Password);
        if (usuario is null)
        {
            ModelState.AddModelError("", "DNI o contraseña incorrectos, o el usuario está deshabilitado.");
            return View(form);
        }

        // Si ya pasaron 7 dias desde el ultimo cambio, la obligamos a cambiarla antes de entrar
        if (_usuarios.DebeCambiarPassword(usuario))
        {
            return RedirectToAction(nameof(CambiarPassword), new { dni = usuario.Dni });
        }

        await IniciarSesionAsync(usuario);
        return RedirectToAction("Index", "Recorrido");
    }

    [AllowAnonymous]
    public IActionResult CambiarPassword(string dni) =>
        View(new CambiarPasswordForm { Dni = dni });

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordForm form)
    {
        if (form.PasswordNueva != form.PasswordNuevaConfirmar)
        {
            ModelState.AddModelError("", "La contraseña nueva no coincide en ambos campos.");
            return View(form);
        }
        if (form.PasswordNueva.Length < 4)
        {
            ModelState.AddModelError("", "La contraseña nueva debe tener al menos 4 caracteres.");
            return View(form);
        }

        var ok = await _usuarios.CambiarPasswordAsync(form.Dni.Trim(), form.PasswordActual, form.PasswordNueva);
        if (!ok)
        {
            ModelState.AddModelError("", "La contraseña actual no es correcta.");
            return View(form);
        }

        TempData["Mensaje"] = "Contraseña actualizada. Ya puedes iniciar sesión con la nueva.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult OlvidePassword() => View(new OlvidePasswordForm());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OlvidePassword(OlvidePasswordForm form)
    {
        var dni = (form.Dni ?? "").Trim();
        var existe = await _usuarios.ExisteUsuarioActivoAsync(dni);
        if (!existe)
        {
            ModelState.AddModelError("", "No se encontró un usuario activo con ese DNI.");
            return View(form);
        }

        await _usuarios.ResetearPasswordAsync(dni);

        TempData["Mensaje"] = "Tu contraseña fue restablecida. Ahora es tu DNI. Al iniciar sesión, el sistema te pedirá crear una nueva.";
        return RedirectToAction(nameof(Login));
    }

    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private async Task IniciarSesionAsync(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Dni),
            new Claim(ClaimTypes.Name, usuario.Nombres),
            new Claim(ClaimTypes.Role, usuario.EsAdministrador ? "Administrador" : "Operador"),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
