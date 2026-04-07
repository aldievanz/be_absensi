using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SmartAttendanceApi.Data;
using SmartAttendanceApi.DTOs;

namespace SmartAttendanceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>GET api/dashboard/user</summary>
    [HttpGet("user")]
    public async Task<IActionResult> GetUserDashboard()
    {
        var userId = GetUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var currentMonth = DateTime.UtcNow.AddHours(7).Month;
        var currentYear = DateTime.UtcNow.AddHours(7).Year;

        // Get today's attendance
        var todayAttendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == today);

        // Get monthly stats
        var monthlyStats = await _context.Attendances
            .Where(a => a.UserId == userId && a.Date.Month == currentMonth && a.Date.Year == currentYear)
            .ToListAsync();

        // Get recent history (last 10 records)
        var recentHistory = await _context.Attendances
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Date)
            .Take(10)
            .Select(a => new AttendanceDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Date = a.Date.ToString("dd MMM yyyy"),
                CheckIn = a.CheckIn != null ? a.CheckIn.Value.ToString(@"hh\:mm") : null,
                CheckOut = a.CheckOut != null ? a.CheckOut.Value.ToString(@"hh\:mm") : null,
                Status = a.Status,
                LocationIn = a.LocationIn,
                PhotoIn = a.PhotoIn,
                PhotoOut = a.PhotoOut,
                Notes = a.Notes
            })
            .ToListAsync();

        // Get work schedule from settings
        var settingsList = await _context.AppSettings.ToListAsync();
        var jamMasukSetting = settingsList.FirstOrDefault(s => s.Key == "jam_masuk" || s.Key == "WorkStartTime");
        var jamPulangSetting = settingsList.FirstOrDefault(s => s.Key == "jam_pulang" || s.Key == "WorkEndTime");
        var hariKerjaSetting = settingsList.FirstOrDefault(s => s.Key == "hari_kerja");
        
        var workStartTime = jamMasukSetting?.Value ?? "09:00";
        var workEndTime = jamPulangSetting?.Value ?? "18:00";
        var hariKerja = hariKerjaSetting?.Value ?? "senin,selasa,rabu,kamis,jumat";
        
        if (TimeOnly.TryParse(workStartTime, out var parsedStart)) workStartTime = parsedStart.ToString(@"HH\:mm");
        if (TimeOnly.TryParse(workEndTime, out var parsedEnd)) workEndTime = parsedEnd.ToString(@"HH\:mm");

        // Calculate attendance stats
        var totalHadir = monthlyStats.Count(a => a.Status == "hadir");
        var totalTelat = monthlyStats.Count(a => a.Status == "telat");
        var totalIzin = monthlyStats.Count(a => a.Status == "izin" || a.Status == "sakit" || a.Status == "cuti");
        var totalAlpha = monthlyStats.Count(a => a.Status == "alpha");

        // Calculate working days in current month (excluding weekends)
        var workingDays = 0;
        for (int day = 1; day <= Math.Min(DateTime.UtcNow.AddHours(7).Day, DateTime.DaysInMonth(currentYear, currentMonth)); day++)
        {
            var date = new DateTime(currentYear, currentMonth, day);
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                workingDays++;
            }
        }

        // Calculate rates
        var attendedDays = totalHadir + totalTelat + totalIzin;
        var attendanceRate = workingDays > 0 ? (int)(attendedDays * 100.0 / workingDays) : 0;
        var onTimeRate = (totalHadir + totalTelat) > 0 ? (int)(totalHadir * 100.0 / (totalHadir + totalTelat)) : 100;

        // Ensure rates are within 0-100
        attendanceRate = Math.Max(0, Math.Min(100, attendanceRate));
        onTimeRate = Math.Max(0, Math.Min(100, onTimeRate));

        var dashboard = new DashboardUserDto
        {
            TodayStatus = todayAttendance?.CheckIn != null
                ? (todayAttendance.CheckOut != null ? "selesai" : todayAttendance.Status)
                : "belum_absen",
            LastCheckIn = todayAttendance?.CheckIn?.ToString(@"hh\:mm"),
            LastCheckOut = todayAttendance?.CheckOut?.ToString(@"hh\:mm"),
            TotalHadir = totalHadir,
            TotalTelat = totalTelat,
            TotalIzin = totalIzin,
            TotalAlpha = totalAlpha,
            RecentHistory = recentHistory,
            AttendanceRate = attendanceRate,
            OnTimeRate = onTimeRate,
            WorkStartTime = workStartTime,
            WorkEndTime = workEndTime,
            HariKerja = hariKerja
        };

        return Ok(ApiResponse<DashboardUserDto>.Ok(dashboard));
    }

    /// <summary>GET api/dashboard/admin</summary>
    [HttpGet("admin")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        var totalKaryawan = await _context.Users.CountAsync(u => u.Role == "user" && u.IsActive);

        var todayAttendances = await _context.Attendances
            .Include(a => a.User)
            .Where(a => a.Date == today)
            .ToListAsync();

        var hadirHariIni = todayAttendances.Count(a => a.Status == "hadir" && a.CheckIn != null);
        var telatHariIni = todayAttendances.Count(a => a.Status == "telat");
        var izinHariIni = todayAttendances.Count(a => a.Status == "izin" || a.Status == "sakit" || a.Status == "cuti");

        var pendingLeave = await _context.LeaveRequests
            .CountAsync(l => l.Status == "pending");

        var recentAttendances = todayAttendances
            .OrderByDescending(a => a.CheckIn)
            .Take(10)
            .Select(a => new AttendanceDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User?.Name,
                Date = a.Date.ToString("yyyy-MM-dd"),
                CheckIn = a.CheckIn?.ToString(@"hh\:mm"),
                CheckOut = a.CheckOut?.ToString(@"hh\:mm"),
                Status = a.Status,
                LocationIn = a.LocationIn,
                PhotoIn = a.PhotoIn,
                PhotoOut = a.PhotoOut
            })
            .ToList();

        // Get work schedule from settings
        var settingsList = await _context.AppSettings.ToListAsync();
        var jamMasukSetting = settingsList.FirstOrDefault(s => s.Key == "jam_masuk" || s.Key == "WorkStartTime");
        var jamPulangSetting = settingsList.FirstOrDefault(s => s.Key == "jam_pulang" || s.Key == "WorkEndTime");
        var hariKerjaSetting = settingsList.FirstOrDefault(s => s.Key == "hari_kerja");
        
        var workStartTime = jamMasukSetting?.Value ?? "09:00";
        var workEndTime = jamPulangSetting?.Value ?? "18:00";
        var hariKerja = hariKerjaSetting?.Value ?? "senin,selasa,rabu,kamis,jumat";
        
        if (TimeOnly.TryParse(workStartTime, out var parsedStart)) workStartTime = parsedStart.ToString(@"HH\:mm");
        if (TimeOnly.TryParse(workEndTime, out var parsedEnd)) workEndTime = parsedEnd.ToString(@"HH\:mm");

        var dashboard = new DashboardAdminDto
        {
            TotalKaryawan = totalKaryawan,
            HadirHariIni = hadirHariIni,
            TelatHariIni = telatHariIni,
            TidakHadirHariIni = totalKaryawan - todayAttendances.Count,
            IzinHariIni = izinHariIni,
            PendingLeave = pendingLeave,
            RecentAttendances = recentAttendances,
            WorkStartTime = workStartTime,
            WorkEndTime = workEndTime,
            HariKerja = hariKerja
        };

        return Ok(ApiResponse<DashboardAdminDto>.Ok(dashboard));
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
}
