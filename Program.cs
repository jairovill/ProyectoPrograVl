using DinkToPdf.Contracts;
using DinkToPdf;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using ProyectoPrograVI.Data;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Configuración de servicios
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// Configuración de sesión
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// En tu Program.cs, agrega estas líneas antes de var app = builder.Build();

builder.Services.AddScoped<PedidoADORepository>(provider =>
    new PedidoADORepository(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<CarritoADORepository>(provider =>
    new CarritoADORepository(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<MetodoPagoADORepository>(provider =>
    new MetodoPagoADORepository(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

builder.Services.AddHttpContextAccessor();

// Autenticación basada en cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

// Autorización
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("Admin", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c =>
                c.Type == "rol_id" && c.Value == "1003")));

    options.AddPolicy("Cliente", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c =>
                c.Type == "rol_id" && c.Value == "5")));
});

var app = builder.Build();

// Configurar pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();