using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using Microsoft.AspNetCore.Identity;
using CitasEPS.Models;
using CitasEPS.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity.UI.Services;
using Resend;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Añadir servicios al contenedor.

// Configuración DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Cadena de conexión 'DefaultConnection' no encontrada.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)); // Usar proveedor apropiado (UseSqlite, UseNpgsql, etc.) si no es SQL Server

// Añadir servicios de Identity
builder.Services.AddIdentity<User, IdentityRole<int>>(options => options.SignIn.RequireConfirmedAccount = true) // Usando nuestra clase User y especificando el tipo de Rol
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<CustomUserClaimsPrincipalFactory>();

// Registrar IEmailSender
builder.Services.AddTransient<IEmailSender, EmailSender>();

// Register Resend services for dependency injection
builder.Services.AddOptions<ResendClientOptions>();
builder.Services.AddHttpClient<IResend, ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
{
    o.ApiToken = builder.Configuration["Resend:ApiKey"] ?? 
        throw new InvalidOperationException("Resend API key not found in configuration.");
});

// Añadir políticas de Autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireDoctorRole", policy => policy.RequireRole("Doctor"));
    options.AddPolicy("RequirePatientRole", policy => policy.RequireRole("Patient"));
    // Mantener la política original "Admin" por si se usa en otro lugar, o eliminarla si es redundante.
    options.AddPolicy("Admin", policy => policy.RequireClaim("IsAdmin", "true"));
});

// Configurar opciones de Razor Pages para soportar áreas de Identity
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
    options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
});

// Configure Data Protection to persist keys to the filesystem
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys");
// Ensure the directory exists
System.IO.Directory.CreateDirectory(keysFolder);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("VitalCare"); // Sets a unique name for the app to isolate its keys

var app = builder.Build();

// Aplicar migraciones automáticamente al inicio (recomendado en Desarrollo)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<ApplicationDbContext>();

    try
    {
        logger.LogInformation("Intentando migrar base de datos...");

        // Lógica de reintento para conexión/migración de BD
        int maxRetries = 5;
        int delaySeconds = 5;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await context.Database.MigrateAsync();
                logger.LogInformation("Migración de base de datos exitosa.");
                break; // Salir del bucle si es exitoso
            }
            catch (Exception exInner)
            {
                logger.LogWarning(exInner, "Error migrando BD en intento {AttemptNumber}. Reintentando en {DelaySeconds}s...", i + 1, delaySeconds);
                if (i == maxRetries - 1) // Si el último intento falló, relanzar
                {
                    logger.LogError("Migración de base de datos falló después de {MaxRetries} intentos.", maxRetries);
                    throw;
                }
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }

        // Inicializar datos (seed) después de migración exitosa
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        await SeedData.Initialize(services, context, userManager, roleManager, logger);

    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ocurrió un error configurando la base de datos.");
        // Decidir si la app debe detenerse o continuar si la configuración falla
        // Para Docker, podría ser mejor dejar que falle si la BD no está disponible
        throw; // Relanzar para potencialmente detener el contenedor en mal estado
    }
}

// Configurar el pipeline de solicitud HTTP.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // El valor HSTS predeterminado es 30 días. Puede cambiar esto para escenarios de producción, ver https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Añadir middleware de Autenticación *antes* de Autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
