using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using Microsoft.AspNetCore.Identity;
using CitasEPS.Models;
using CitasEPS.Models.Modules.Users;
using CitasEPS.Models.Modules.Medical;
using CitasEPS.Models.Modules.Appointments;
using CitasEPS.Models.Modules.Core;
using CitasEPS.Services;
using CitasEPS.Services.Modules.Common;
using Microsoft.AspNetCore.Identity.UI.Services;
using Resend;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Añadir servicios al contenedor.

// Configuración DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Cadena de conexión 'DefaultConnection' no encontrada.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)); // Usar proveedor apropiado (UseSqlite, UseNpgsql, etc.) si no es SQL Server

// Añadir servicios de Identity
builder.Services.AddIdentity<User, IdentityRole<int>>(options => 
{
    // En desarrollo, desactivamos la confirmación requerida para facilitar las pruebas
    options.SignIn.RequireConfirmedAccount = !builder.Environment.IsDevelopment();
    
    // Configuración más simple para contraseñas
    options.Password.RequireDigit = false;           // No requerir dígitos
    options.Password.RequireLowercase = false;       // No requerir minúsculas
    options.Password.RequireUppercase = false;       // No requerir mayúsculas
    options.Password.RequireNonAlphanumeric = false; // No requerir caracteres especiales
    options.Password.RequiredLength = 4;             // Solo requerir mínimo 4 caracteres
    options.Password.RequiredUniqueChars = 1;        // Solo requerir 1 carácter único
    
    // Configuración de tokens
    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
    options.Tokens.ChangeEmailTokenProvider = TokenOptions.DefaultEmailProvider;
}) // Usando nuestra clase User y especificando el tipo de Rol
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddRoles<IdentityRole<int>>()
    .AddClaimsPrincipalFactory<CustomUserClaimsPrincipalFactory>();

// Configurar token providers con tiempo de vida extendido
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromDays(1); // Los tokens de confirmación de email duran 1 día
});

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
    options.AddPolicy("RequirePatientRole", policy => policy.RequireRole("Paciente"));
    // Mantener la política original "Admin" por si se usa en otro lugar, o eliminarla si es redundante.
    options.AddPolicy("Admin", policy => policy.RequireClaim("IsAdmin", "true"));
});

// <<< INICIO: Configuración de Cookie Authentication >>>
builder.Services.ConfigureApplicationCookie(options =>
{
    // Asegurar que las rutas de redirección sean correctas
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});
// <<< FIN: Configuración de Cookie Authentication >>>

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
    .SetApplicationName("VitalCare") // Sets a unique name for the app to isolate its keys
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90)); // Keys are valid for 90 days

// Register custom application services
builder.Services.AddScoped<IDateTimeService, DateTimeService>();
builder.Services.AddScoped<IAppointmentPolicyService, AppointmentPolicyService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAppointmentEmailService, AppointmentEmailService>();

builder.Services.AddControllers(); // <<< Add this for API controllers

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

        // Ejecutar script de corrección de columnas faltantes
        logger.LogInformation("Ejecutando script de corrección de columnas faltantes...");
        try
        {
            // Agregar columna Description a Specialties si no existe
            await context.Database.ExecuteSqlRawAsync(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'Specialties' AND column_name = 'Description') THEN
                        ALTER TABLE ""Specialties"" ADD COLUMN ""Description"" character varying(500);
                    END IF;
                END $$;
            ");

            // Agregar columna IsAvailable a Doctors si no existe
            await context.Database.ExecuteSqlRawAsync(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'Doctors' AND column_name = 'IsAvailable') THEN
                        ALTER TABLE ""Doctors"" ADD COLUMN ""IsAvailable"" boolean NOT NULL DEFAULT TRUE;
                    END IF;
                END $$;
            ");

            // Agregar columna LicenseNumber a Doctors si no existe
            await context.Database.ExecuteSqlRawAsync(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'Doctors' AND column_name = 'LicenseNumber') THEN
                        ALTER TABLE ""Doctors"" ADD COLUMN ""LicenseNumber"" character varying(50);
                    END IF;
                END $$;
            ");

            // Agregar columna RescheduleCount a Appointments si no existe
            await context.Database.ExecuteSqlRawAsync(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'Appointments' AND column_name = 'RescheduleCount') THEN
                        ALTER TABLE ""Appointments"" ADD COLUMN ""RescheduleCount"" integer NOT NULL DEFAULT 0;
                    END IF;
                    
                    -- Agregar nuevas columnas para reagendamientos por separado
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'Appointments' AND column_name = 'PatientRescheduleCount') THEN
                        ALTER TABLE ""Appointments"" ADD COLUMN ""PatientRescheduleCount"" integer NOT NULL DEFAULT 0;
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'Appointments' AND column_name = 'DoctorRescheduleCount') THEN
                        ALTER TABLE ""Appointments"" ADD COLUMN ""DoctorRescheduleCount"" integer NOT NULL DEFAULT 0;
                    END IF;
                END
                $$;");

            // Actualizar todos los doctores existentes para que estén disponibles por defecto
            await context.Database.ExecuteSqlRawAsync(@"
                UPDATE ""Doctors"" SET ""IsAvailable"" = TRUE 
                WHERE ""IsAvailable"" IS NULL OR ""IsAvailable"" = FALSE;
            ");
            
            logger.LogInformation("Script de corrección ejecutado exitosamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error ejecutando script de corrección de columnas.");
            // Continuar sin lanzar excepción para que la app funcione
        }

        // Inicializar datos (seed) después de migración exitosa
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        await SeedData.Initialize(services, context, userManager, roleManager, logger);

        // Fix missing Patient records for users with "Paciente" role
        logger.LogInformation("Running missing Patient records check and fix...");
        await MigrationHelpers.FixMissingPatientRecordsAsync(context, userManager, logger);

        // Optional: Run integrity validation (can be removed in production)
        await MigrationHelpers.ValidateUserPatientIntegrityAsync(context, userManager, logger);

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
app.MapControllers(); // <<< Add this to map API controller routes

app.Run();



