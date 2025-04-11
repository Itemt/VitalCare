using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;
using Microsoft.AspNetCore.Identity;
using CitasEPS.Models;
using CitasEPS.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add DbContext configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)); // Use appropriate provider (UseSqlite, UseNpgsql, etc.) if not using SQL Server

// Add Identity services
builder.Services.AddIdentity<User, IdentityRole<int>>(options => options.SignIn.RequireConfirmedAccount = false) // Using our User class and specifying the Role type
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<CustomUserClaimsPrincipalFactory>();

// Register IEmailSender
builder.Services.AddTransient<IEmailSender, EmailSender>();

// Add Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireClaim("IsAdmin", "true"));
    // Add other policies here if needed
});

// Configure Razor Pages options to support Identity areas
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
    options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
});

var app = builder.Build();

// Apply migrations automatically on startup (Development recommended)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<ApplicationDbContext>();

    try
    {
        logger.LogInformation("Attempting to migrate database...");

        // Retry logic for database connection/migration
        int maxRetries = 5;
        int delaySeconds = 5;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migration successful.");
                break; // Exit loop if successful
            }
            catch (Exception exInner)
            {
                logger.LogWarning(exInner, "Error migrating DB on attempt {AttemptNumber}. Retrying in {DelaySeconds}s...", i + 1, delaySeconds);
                if (i == maxRetries - 1) // If last attempt failed, rethrow
                {
                    logger.LogError("Database migration failed after {MaxRetries} attempts.", maxRetries);
                    throw;
                }
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }

        // Optional: Seed data here if needed after successful migration
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred setting up the database.");
        // Decide if the app should stop or continue if setup fails
        // For Docker, it might be better to let it crash if the DB isn't available
        throw; // Rethrow to potentially stop the container from running in a bad state
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add Authentication middleware *before* Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
