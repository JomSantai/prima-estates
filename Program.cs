using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PrimaEstates.Data;
using PrimaEstates.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// --- Database provider selection ---
// Production (Railway): PostgreSQL via DATABASE_URL / connection string.
// Local dev: SQLite file, zero config.
var pgConn = ResolvePostgresConnectionString(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (!string.IsNullOrWhiteSpace(pgConn))
        opt.UseNpgsql(pgConn);
    else
        opt.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=primaestates.db");
});

builder.Services.AddScoped<IImageStorage, ImageStorage>();

// Keep Data Protection keys in the database so antiforgery tokens and auth
// cookies remain valid across redeploys (containers are ephemeral).
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>()
    .SetApplicationName("PrimaEstates");

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/account/login";
        opt.AccessDeniedPath = "/account/login";
        opt.ExpireTimeSpan = TimeSpan.FromHours(8);
        opt.SlidingExpiration = true;
        opt.Cookie.HttpOnly = true;
        opt.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// --- Create schema (if needed) and seed ---
// EnsureCreated builds the full schema from the model on first run - works on
// both SQLite (local) and PostgreSQL (Railway). See README for switching to
// versioned EF migrations once the schema starts evolving in production.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // EnsureCreated() won't add new tables to an already-created database, so
    // make sure the Data Protection key table exists on existing deployments.
    var isNpgsql = db.Database.ProviderName?.Contains("Npgsql") == true;
    var createKeys = isNpgsql
        ? """
          CREATE TABLE IF NOT EXISTS "DataProtectionKeys" (
              "Id" serial PRIMARY KEY,
              "FriendlyName" text NULL,
              "Xml" text NULL
          );
          """
        : """
          CREATE TABLE IF NOT EXISTS "DataProtectionKeys" (
              "Id" INTEGER NOT NULL CONSTRAINT "PK_DataProtectionKeys" PRIMARY KEY AUTOINCREMENT,
              "FriendlyName" TEXT NULL,
              "Xml" TEXT NULL
          );
          """;
    try { db.Database.ExecuteSqlRaw(createKeys); }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Could not ensure DataProtectionKeys table exists.");
    }

    SeedData.Initialize(db, app.Configuration);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/home/error");
    app.UseHsts();
    // Railway terminates TLS at the edge and forwards X-Forwarded-* headers.
    app.UseForwardedHeaders(new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                         | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
    });
}

// Only force HTTPS locally; on Railway the platform handles TLS.
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


// Railway's Postgres plugin exposes DATABASE_URL in the form
// postgresql://user:pass@host:port/db - convert it to an Npgsql key/value string.
static string? ResolvePostgresConnectionString(IConfiguration config)
{
    // 1) Explicit Npgsql-format string wins
    var explicitConn = config.GetConnectionString("Postgres")
                       ?? Environment.GetEnvironmentVariable("Postgres");
    if (!string.IsNullOrWhiteSpace(explicitConn))
        return explicitConn;

    // 2) Railway-style DATABASE_URL
    var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (string.IsNullOrWhiteSpace(dbUrl)) return null;

    try
    {
        var uri = new Uri(dbUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var database = uri.AbsolutePath.TrimStart('/');
        var b = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
            Database = database,
            SslMode = Npgsql.SslMode.Require
        };
        return b.ConnectionString;
    }
    catch
    {
        return null;
    }
}
