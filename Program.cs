using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using SmartAttendanceApi.Data;

var builder = WebApplication.CreateBuilder(args);

// === Database (PostgreSQL / Supabase) ===
var envDbUrl = Environment.GetEnvironmentVariable("DATABASE_URL") ?? Environment.GetEnvironmentVariable("POSTGRES_URL");
string connectionString;

if (!string.IsNullOrEmpty(envDbUrl))
{
    connectionString = ParsePostgresUrl(envDbUrl);
}
else
{
    var configConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
    if (configConn.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || 
        configConn.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        connectionString = ParsePostgresUrl(configConn);
    }
    else
    {
        connectionString = configConn;
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// === JWT Authentication ===
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// === CORS (Allow Frontend) ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Allow any origin (Vercel, localhost, etc.)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();

var app = builder.Build();

// === Middleware Pipeline ===
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (dbContext.Database.CanConnect())
    {
        // Pastikan tabel aplikasi dibuat di database baru (kalau belum ada)
        try
        {
            var databaseCreator = (RelationalDatabaseCreator)dbContext.Database.GetService<IDatabaseCreator>();
            databaseCreator.CreateTables();
        }
        catch
        {
            // Abaikan jika tabel sudah terbuat sebelumnya
        }

        var admin = dbContext.Users.FirstOrDefault(u => u.Email == "admin@example.com");
        if (admin == null)
        {
            dbContext.Users.Add(new SmartAttendanceApi.Models.User { Name = "Administrator", Email = "admin@example.com", Password = BCrypt.Net.BCrypt.HashPassword("password123"), Role = "admin", IsActive = true });
        }
        else
        {
            admin.Password = BCrypt.Net.BCrypt.HashPassword("password123");
        }

        var user = dbContext.Users.FirstOrDefault(u => u.Email == "user@example.com");
        if (user == null)
        {
            dbContext.Users.Add(new SmartAttendanceApi.Models.User { Name = "Pegawai", Email = "user@example.com", Password = BCrypt.Net.BCrypt.HashPassword("password123"), Role = "user", IsActive = true });
        }
        else
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword("password123");
        }

        // --- SEED APP SETTINGS (Pengaturan Default) ---
        var defaultSettings = new Dictionary<string, string>
        {
            { "jam_masuk", "08:00" },
            { "jam_pulang", "17:00" },
            { "office_latitude", "-7.0886413436688445" },
            { "office_longitude", "110.28992953715782" },
            { "office_radius", "100" },
            { "hari_kerja", "senin,selasa,rabu,kamis,jumat" }
        };

        foreach (var ds in defaultSettings)
        {
            var setting = dbContext.AppSettings.FirstOrDefault(s => s.Key == ds.Key);
            if (setting == null)
            {
                dbContext.AppSettings.Add(new SmartAttendanceApi.Models.AppSetting { Key = ds.Key, Value = ds.Value });
            }
        }

        dbContext.SaveChanges();
    }
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// === Bind ke PORT dari Railway (atau default 5210 lokal) ===
var port = Environment.GetEnvironmentVariable("PORT") ?? "5210";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();

// === Helper function to parse Postgres URL/URI ===
static string ParsePostgresUrl(string url)
{
    try
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':');
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        if (string.IsNullOrEmpty(database)) database = "postgres";
        
        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};Pooling=true;SSL Mode=Require;Trust Server Certificate=true;";
    }
    catch
    {
        return url;
    }
}
