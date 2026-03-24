using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SmartAttendanceApi.Data;
using SmartAttendanceApi.DTOs;
using SmartAttendanceApi.Models;

namespace SmartAttendanceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly AppDbContext _context;

    public AttendanceController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>DEV ONLY: RESET ATTENDANCE HARI INI</summary>
    [HttpGet("reset-dev")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetDev()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var attendances = await _context.Attendances.Where(a => a.Date == today).ToListAsync();
        _context.Attendances.RemoveRange(attendances);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Riwayat absen test HARI INI berhasil dilenyapkan! Silakan test check-in dengan kamera lagi." });
    }

    /// <summary>POST api/attendance/check-in</summary>
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
    {
        var userId = GetUserId();
        var today = DateOnly.FromDateTime(DateTime.Now);

        // Cek apakah sudah absen hari ini
        var existing = await _context.Attendances
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == today);

        if (existing != null && existing.CheckIn != null)
            return BadRequest(ApiResponse<string>.Fail("Kamu sudah check-in hari ini"));

        // Ambil setting jam masuk
        var jamMasukSetting = await _context.AppSettings
            .FirstOrDefaultAsync(s => s.Key == "jam_masuk");
        var jamMasuk = TimeOnly.Parse(jamMasukSetting?.Value ?? "08:00");

        var now = TimeOnly.FromDateTime(DateTime.Now);
        var status = now > jamMasuk ? "telat" : "hadir";

        // Validasi radius lokasi wajib menyala
        if (!request.Latitude.HasValue || !request.Longitude.HasValue)
        {
            return BadRequest(ApiResponse<string>.Fail("Lokasi tidak terdeteksi. Wajib mengaktifkan GPS."));
        }

        if (string.IsNullOrEmpty(request.Photo))
        {
            return BadRequest(ApiResponse<string>.Fail("Wajib jepret foto presensi sebelum check-in!"));
        }

        var isInRadius = await ValidateLocation(request.Latitude.Value, request.Longitude.Value);
        if (!isInRadius)
            return BadRequest(ApiResponse<string>.Fail("Lokasi kamu di luar radius kantor"));

        if (existing != null)
        {
            existing.CheckIn = now;
            existing.Status = status;
            existing.LocationIn = request.LocationName;
            existing.LatitudeIn = request.Latitude;
            existing.LongitudeIn = request.Longitude;
            existing.PhotoIn = request.Photo;
            existing.Notes = request.Notes;
        }
        else
        {
            var attendance = new Attendance
            {
                UserId = userId,
                Date = today,
                CheckIn = now,
                Status = status,
                LocationIn = request.LocationName,
                LatitudeIn = request.Latitude,
                LongitudeIn = request.Longitude,
                PhotoIn = request.Photo,
                Notes = request.Notes
            };
            _context.Attendances.Add(attendance);
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            status,
            checkIn = now.ToString("HH:mm:ss"),
            message = status == "telat" ? $"Kamu telat! Jam masuk: {jamMasuk}" : "Absen masuk berhasil!"
        }));
    }

    /// <summary>POST api/attendance/check-out</summary>
    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request)
    {
        var userId = GetUserId();
        var today = DateOnly.FromDateTime(DateTime.Now);

        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == today);

        if (attendance == null || attendance.CheckIn == null)
            return BadRequest(ApiResponse<string>.Fail("Kamu belum check-in hari ini"));

        if (attendance.CheckOut != null)
            return BadRequest(ApiResponse<string>.Fail("Kamu sudah check-out hari ini"));

        // Validasi radius lokasi wajib menyala
        if (!request.Latitude.HasValue || !request.Longitude.HasValue)
        {
            return BadRequest(ApiResponse<string>.Fail("Lokasi tidak terdeteksi. Wajib mengaktifkan GPS."));
        }

        if (string.IsNullOrEmpty(request.Photo))
        {
            return BadRequest(ApiResponse<string>.Fail("Wajib jepret foto presensi sebelum check-out!"));
        }

        var isInRadius = await ValidateLocation(request.Latitude.Value, request.Longitude.Value);
        if (!isInRadius)
            return BadRequest(ApiResponse<string>.Fail("Lokasi kamu di luar radius kantor"));

        var now = TimeOnly.FromDateTime(DateTime.Now);
        attendance.CheckOut = now;
        attendance.LocationOut = request.LocationName;
        attendance.LatitudeOut = request.Latitude;
        attendance.LongitudeOut = request.Longitude;
        attendance.PhotoOut = request.Photo;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            checkOut = now.ToString("HH:mm:ss"),
            message = "Absen pulang berhasil!"
        }));
    }

    /// <summary>GET api/attendance/today</summary>
    [HttpGet("today")]
    public async Task<IActionResult> GetToday()
    {
        var userId = GetUserId();
        var today = DateOnly.FromDateTime(DateTime.Now);

        var attendance = await _context.Attendances
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == today);

        if (attendance == null)
            return Ok(ApiResponse<object>.Ok(new { status = "belum_absen" }));

        return Ok(ApiResponse<AttendanceDto>.Ok(MapToDto(attendance)));
    }

    /// <summary>GET api/attendance/history?month=3&year=2026&targetUserId=...</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int? month, [FromQuery] int? year, [FromQuery] int? targetUserId)
    {
        var loggedInUserId = GetUserId();
        var m = month ?? DateTime.Now.Month;
        var y = year ?? DateTime.Now.Year;

        var role = User.FindFirstValue(ClaimTypes.Role);

        IQueryable<Attendance> query = _context.Attendances
            .Include(a => a.User)
            .Where(a => a.Date.Month == m && a.Date.Year == y);

        if (role == "admin")
        {
            // Admin: if targetUserId specified, filter by that user; otherwise show ALL
            if (targetUserId.HasValue)
            {
                query = query.Where(a => a.UserId == targetUserId.Value);
            }
        }
        else
        {
            // Regular user: only their own data
            query = query.Where(a => a.UserId == loggedInUserId);
        }

        var attendances = await query
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.CheckIn)
            .ToListAsync();

        return Ok(ApiResponse<List<AttendanceDto>>.Ok(
            attendances.Select(MapToDto).ToList()));
    }

    /// <summary>GET api/attendance/all (Admin only)</summary>
    [HttpGet("all")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var targetDate = string.IsNullOrEmpty(date)
            ? DateOnly.FromDateTime(DateTime.Now)
            : DateOnly.Parse(date);

        var query = _context.Attendances
            .Include(a => a.User)
            .Where(a => a.Date == targetDate)
            .OrderByDescending(a => a.CheckIn);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new PaginatedResponse<AttendanceDto>
        {
            Results = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PaginatedResponse<AttendanceDto>>.Ok(response));
    }

    /// <summary>GET api/attendance/report?month=3&year=2026 (Admin only)</summary>
    [HttpGet("report")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetReport([FromQuery] int? month, [FromQuery] int? year)
    {
        var m = month ?? DateTime.Now.Month;
        var y = year ?? DateTime.Now.Year;

        var attendances = await _context.Attendances
            .Include(a => a.User)
            .Where(a => a.Date.Month == m && a.Date.Year == y)
            .OrderBy(a => a.User!.Name)
            .ThenBy(a => a.Date)
            .ToListAsync();

        return Ok(ApiResponse<List<AttendanceDto>>.Ok(
            attendances.Select(MapToDto).ToList()));
    }

    private async Task<bool> ValidateLocation(double lat, double lng)
    {
        var latSetting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == "office_latitude");
        var lngSetting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == "office_longitude");
        var radiusSetting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == "office_radius");

        if (latSetting == null || lngSetting == null) return true; // skip jika belum di-set

        var officeLat = double.Parse(latSetting.Value, System.Globalization.CultureInfo.InvariantCulture);
        var officeLng = double.Parse(lngSetting.Value, System.Globalization.CultureInfo.InvariantCulture);
        var radius = double.Parse(radiusSetting?.Value ?? "100", System.Globalization.CultureInfo.InvariantCulture);

        var distance = CalculateDistance(lat, lng, officeLat, officeLng);
        return distance <= radius;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371000; // radius bumi dalam meter
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    private static AttendanceDto MapToDto(Attendance a) => new()
    {
        Id = a.Id,
        UserId = a.UserId,
        UserName = a.User?.Name,
        Date = a.Date.ToString("yyyy-MM-dd"),
        CheckIn = a.CheckIn?.ToString("HH:mm:ss"),
        CheckOut = a.CheckOut?.ToString("HH:mm:ss"),
        Status = a.Status,
        LocationIn = a.LocationIn,
        LocationOut = a.LocationOut,
        LatitudeIn = a.LatitudeIn,
        LongitudeIn = a.LongitudeIn,
        PhotoIn = a.PhotoIn,
        PhotoOut = a.PhotoOut,
        Notes = a.Notes
    };
}
