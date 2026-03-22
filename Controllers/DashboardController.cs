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
        var today = DateOnly.FromDateTime(DateTime.Now);
        var currentMonth = DateTime.Now.Month;
        var currentYear = DateTime.Now.Year;

        var todayAttendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == today);

        var monthlyStats = await _context.Attendances
            .Where(a => a.UserId == userId && a.Date.Month == currentMonth && a.Date.Year == currentYear)
            .ToListAsync();

        var dashboard = new DashboardUserDto
        {
            TodayStatus = todayAttendance?.CheckIn != null
                ? (todayAttendance.CheckOut != null ? "selesai" : todayAttendance.Status)
                : "belum_absen",
            LastCheckIn = todayAttendance?.CheckIn?.ToString("HH:mm"),
            LastCheckOut = todayAttendance?.CheckOut?.ToString("HH:mm"),
            TotalHadir = monthlyStats.Count(a => a.Status == "hadir"),
            TotalTelat = monthlyStats.Count(a => a.Status == "telat"),
            TotalIzin = monthlyStats.Count(a => a.Status == "izin" || a.Status == "sakit" || a.Status == "cuti"),
            TotalAlpha = monthlyStats.Count(a => a.Status == "alpha")
        };

        return Ok(ApiResponse<DashboardUserDto>.Ok(dashboard));
    }

    /// <summary>GET api/dashboard/admin</summary>
    [HttpGet("admin")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

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
                CheckIn = a.CheckIn?.ToString("HH:mm:ss"),
                CheckOut = a.CheckOut?.ToString("HH:mm:ss"),
                Status = a.Status,
                LocationIn = a.LocationIn,
                PhotoIn = a.PhotoIn,
                PhotoOut = a.PhotoOut
            })
            .ToList();

        var dashboard = new DashboardAdminDto
        {
            TotalKaryawan = totalKaryawan,
            HadirHariIni = hadirHariIni,
            TelatHariIni = telatHariIni,
            TidakHadirHariIni = totalKaryawan - todayAttendances.Count,
            IzinHariIni = izinHariIni,
            PendingLeave = pendingLeave,
            RecentAttendances = recentAttendances
        };

        return Ok(ApiResponse<DashboardAdminDto>.Ok(dashboard));
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
}
