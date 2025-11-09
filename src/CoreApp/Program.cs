#if NET7_0
using African_Beauty_Trading.CoreApp;
using African_Beauty_Trading.CoreApp.Hubs;
using African_Beauty_Trading.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Allow overriding the connection string via environment variable for containers
var envConn = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");
var configConn = builder.Configuration.GetConnectionString("DefaultConnection");
var defaultConn = "Host=localhost;Database=africanbeauty;Username=postgres;Password=postgres";
var connectionString = !string.IsNullOrEmpty(envConn) ? envConn : (configConn ?? defaultConn);
Console.WriteLine($"Using connection string: {(string.IsNullOrEmpty(envConn) ? "appsettings/DefaultConnection" : "ENV:CONNECTION_STRING") }");

// Use Npgsql provider for Postgres
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
builder.Services.AddDbContext<African_Beauty_Trading.CoreApp.ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<African_Beauty_Trading.CoreApp.ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add SignalR
builder.Services.AddSignalR();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Development-only: ensure database is created for local testing when migrations are not yet applied
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
            Console.WriteLine("Database EnsureCreated executed (Development environment).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database EnsureCreated failed: {ex.Message}");
        }
    }
}

// Optional: apply EF Core migrations automatically when requested (use with caution in production)
var applyMigrations = Environment.GetEnvironmentVariable("APPLY_MIGRATIONS");
if (!string.IsNullOrEmpty(applyMigrations) && applyMigrations.Equals("true", StringComparison.OrdinalIgnoreCase))
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
            Console.WriteLine("Database migrations applied via APPLY_MIGRATIONS=true");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database Migrate failed: {ex.Message}");
        }
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map SignalR hub
app.MapHub<ChatHub>("/chathub");

// Bind to Railway/Heroku style PORT env var when provided
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portEnv))
{
    Console.WriteLine($"Binding to PORT={portEnv}");
    app.Urls.Clear();
    app.Urls.Add($"http://*:{portEnv}");
}
else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    Console.WriteLine($"Using ASPNETCORE_URLS={Environment.GetEnvironmentVariable("ASPNETCORE_URLS")}");
}
else
{
    // default to port 80 inside container
    app.Urls.Add("http://*:80");
}

app.Run();
#endif
