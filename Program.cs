using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<TrasladosPeruWeb.Data.ConexionFactory>();
builder.Services.AddScoped<TrasladosPeruWeb.Repositories.RecorridoRepository>();
builder.Services.AddScoped<TrasladosPeruWeb.Repositories.UsuarioRepository>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Login";
        options.AccessDeniedPath = "/Cuenta/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles(); // sirve los archivos de wwwroot (css, imagenes)
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Ruta por defecto: si no se especifica nada, entra a Home/Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
