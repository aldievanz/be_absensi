using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartAttendanceApi.Data;

var builder = WebApplication.CreateBuilder(args);

// === Database (MySQL) ===
// Prioritas: Environment Variable MYSQL_URL (Railway) > appsettings.json
var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL");
string connectionString;
if (!string.IsNullOrEmpty(mysqlUrl))
{
    // Parse MYSQL_URL format: mysql://user:password@host:port/database
    var uri = new Uri(mysqlUrl);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Server={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};User={userInfo[0]};Password={userInfo[1]};";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
}
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

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
        // Pastikan tabel dibuat di database baru (kalau belum ada)
        dbContext.Database.EnsureCreated();

        // === AUTO UPDATE DATABASE SCHEMA ===
        try
        {
            dbContext.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN photo_profile LONGTEXT;");
        }
        catch { /* Ignore if column exists */ }
        
        try
        {
            dbContext.Database.ExecuteSqlRaw("ALTER TABLE attendances MODIFY photo_in LONGTEXT;");
            dbContext.Database.ExecuteSqlRaw("ALTER TABLE attendances MODIFY photo_out LONGTEXT;");
        }
        catch { /* Ignore */ }

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

        // --- UPDATE PENGATURAN GPS ABSENSI ---
        var setLat = dbContext.AppSettings.FirstOrDefault(s => s.Key == "office_latitude");
        if (setLat != null) setLat.Value = "-7.0886413436688445";

        var setLng = dbContext.AppSettings.FirstOrDefault(s => s.Key == "office_longitude");
        if (setLng != null) setLng.Value = "110.28992953715782";

        var setRad = dbContext.AppSettings.FirstOrDefault(s => s.Key == "office_radius");
        if (setRad != null) setRad.Value = "100";

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
