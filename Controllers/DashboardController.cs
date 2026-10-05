using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SmartAttendanceApi.Data;
using SmartAttendanceApi.DTOs;
using SmartAttendanceApi.Services;

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

    // ─── PUNCTUALITY RATE ───────────────────────────────────────────────────

    /// <summary>
    /// GET /api/dashboard/punctuality?period=week|month&amp;date=YYYY-MM-DD[&amp;userId=int]
    /// Juga dapat diakses via GET /dashboard/punctuality
    ///
    /// Karyawan: hanya data diri sendiri.
    /// Admin tanpa userId: ringkasan + tabel seluruh karyawan aktif (urut terendah).
    /// Admin dengan userId: data satu karyawan (format PunctualityDto).
    /// </summary>
    [HttpGet("punctuality")]
    [HttpGet("/dashboard/punctuality")]
    public async Task<IActionResult> GetPunctuality(
        [FromQuery] string period = "week",
        [FromQuery] string? date = null,
        [FromQuery] int? userId = null)
    {
        // ── Validasi period ──────────────────────────────────────────────
        period = period.ToLowerInvariant();
        if (period != "week" && period != "month")
            return BadRequest(ApiResponse<string>.Fail("Parameter 'period' harus 'week' atau 'month'."));

        // ── Tentukan tanggal referensi (default: hari ini WIB) ──────────
        DateOnly refDate;
        if (date == null)
        {
            refDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        }
        else if (!DateOnly.TryParseExact(date, "yyyy-MM-dd",
                     System.Globalization.CultureInfo.InvariantCulture,
                     System.Globalization.DateTimeStyles.None, out refDate))
        {
            return BadRequest(ApiResponse<string>.Fail("Format 'date' harus YYYY-MM-DD."));
        }

        // ── Ambil pengaturan sistem (hari kerja & jam masuk) ─────────────
        var settingsList = await _context.AppSettings.ToListAsync();
        var hariKerjaSetting = settingsList.FirstOrDefault(s => s.Key == "hari_kerja");
        var workDayNames = PunctualityCalculator.ParseWorkDays(hariKerjaSetting?.Value);

        var jamMasukSetting = settingsList.FirstOrDefault(s => s.Key == "jam_masuk" || s.Key == "WorkStartTime");
        var jamMasukStr = jamMasukSetting?.Value ?? "09:00";
        if (!TimeOnly.TryParse(jamMasukStr, out var jamMasuk))
        {
            jamMasuk = new TimeOnly(9, 0);
        }

        // ── Tentukan rentang periode & periode sebelumnya ─────────────────
        var (from, to) = PunctualityCalculator.GetPeriodRange(period, refDate);
        var (prevFrom, prevTo) = PunctualityCalculator.GetPreviousPeriodRange(period, refDate);

        var nowWib = DateTime.UtcNow.AddHours(7);
        var today = DateOnly.FromDateTime(nowWib);
        var nowTime = TimeOnly.FromDateTime(nowWib);

        var role = User.FindFirstValue(ClaimTypes.Role);
        var loggedInUserId = GetUserId();

        // ── ADMIN TANPA userId: ringkasan seluruh karyawan ────────────────
        if (role == "admin" && userId == null)
        {
            return await GetAdminPunctualitySummary(
                period, from, to, prevFrom, prevTo,
                workDayNames, today, nowTime, jamMasuk);
        }

        // ── Tentukan target user ──────────────────────────────────────────
        int targetUserId;
        if (role == "admin" && userId.HasValue)
        {
            targetUserId = userId.Value;
            var userExists = await _context.Users.AnyAsync(u => u.Id == targetUserId);
            if (!userExists)
                return NotFound(ApiResponse<string>.Fail("Karyawan tidak ditemukan."));
        }
        else
        {
            // Karyawan hanya bisa melihat dirinya sendiri
            targetUserId = loggedInUserId;
        }

        // ── Hitung untuk satu user ────────────────────────────────────────
        var dto = await ComputeUserPunctualityDto(
            targetUserId, period, from, to, prevFrom, prevTo,
            workDayNames, today, nowTime, jamMasuk);

        return Ok(ApiResponse<PunctualityDto>.Ok(dto));
    }

    // ─── Ringkasan admin semua karyawan ──────────────────────────────────────
    private async Task<IActionResult> GetAdminPunctualitySummary(
        string period,
        DateOnly from, DateOnly to,
        DateOnly prevFrom, DateOnly prevTo,
        HashSet<DayOfWeek> workDayNames,
        DateOnly today, TimeOnly nowTime, TimeOnly jamMasuk)
    {
        // Ambil semua karyawan aktif (role=user)
        var activeUsers = await _context.Users
            .Where(u => u.IsActive && u.Role == "user")
            .Select(u => new { u.Id, u.Name, u.Department, u.PhotoProfile })
            .ToListAsync();

        if (!activeUsers.Any())
        {
            return Ok(ApiResponse<PunctualityAdminDto>.Ok(new PunctualityAdminDto
            {
                Period = period,
                Range = new DateRangeDto
                {
                    From = from.ToString("yyyy-MM-dd"),
                    To = to.ToString("yyyy-MM-dd")
                }
            }));
        }

        var userIds = activeUsers.Select(u => u.Id).ToList();

        // Satu query untuk semua attendance (period saat ini + sebelumnya) - cegah N+1
        var minDate = prevFrom < from ? prevFrom : from;
        var maxDate = to > prevTo ? to : prevTo;

        var allAttendances = await _context.Attendances
            .Where(a => userIds.Contains(a.UserId) && a.Date >= minDate && a.Date <= maxDate)
            .Select(a => new { a.UserId, a.Date, a.Status, a.CheckIn })
            .ToListAsync();

        var currentAttendances = allAttendances.Where(a => a.Date >= from && a.Date <= to).ToList();
        var prevAttendances = allAttendances.Where(a => a.Date >= prevFrom && a.Date <= prevTo).ToList();

        var summaries = new List<PunctualityUserSummaryDto>();
        var prevPercents = new List<double>();

        foreach (var u in activeUsers)
        {
            var curAtt = currentAttendances.Where(a => a.UserId == u.Id).ToList();

            // Aturan 3: Hari ini dihitung jika sudah check-in ATAU jam masuk sudah lewat
            var hasCheckedInToday = curAtt.Any(a => a.Date == today && a.CheckIn != null);
            var todayCounts = PunctualityCalculator.IsTodayElapsed(hasCheckedInToday, nowTime, jamMasuk);
            var elapsedWorkDays = PunctualityCalculator.CountElapsedWorkDays(from, to, workDayNames, today, todayCounts);

            // Cut-off tanggal efektif
            var effectiveTo = to < today ? to : (todayCounts ? today : today.AddDays(-1));
            var validAtt = curAtt.Where(a => workDayNames.Contains(a.Date.DayOfWeek) && a.Date <= effectiveTo).ToList();

            var onTime = validAtt.Count(a => a.Status == "hadir");
            var late = validAtt.Count(a => a.Status == "telat");
            var excused = validAtt.Count(a => a.Status == "izin" || a.Status == "sakit" || a.Status == "cuti");
            var denominator = Math.Max(0, elapsedWorkDays - excused);
            var absent = Math.Max(0, denominator - onTime - late);
            var pct = PunctualityCalculator.ComputePunctualityPercent(onTime, elapsedWorkDays, excused);

            // Periode sebelumnya
            var prevAtt = prevAttendances.Where(a => a.UserId == u.Id).ToList();
            var prevElapsed = PunctualityCalculator.CountElapsedWorkDays(prevFrom, prevTo, workDayNames, today, true);
            var prevValidAtt = prevAtt.Where(a => workDayNames.Contains(a.Date.DayOfWeek)).ToList();
            var prevOnTime = prevValidAtt.Count(a => a.Status == "hadir");
            var prevExcused = prevValidAtt.Count(a => a.Status == "izin" || a.Status == "sakit" || a.Status == "cuti");
            var prevPct = PunctualityCalculator.ComputePunctualityPercent(prevOnTime, prevElapsed, prevExcused);

            prevPercents.Add(prevPct);

            summaries.Add(new PunctualityUserSummaryDto
            {
                UserId = u.Id,
                UserName = u.Name,
                Department = u.Department,
                PhotoProfile = u.PhotoProfile,
                WorkDays = denominator,
                OnTime = onTime,
                Late = late,
                Absent = absent,
                Excused = excused,
                PunctualityPercent = pct,
            });
        }

        // Rata-rata saat ini dan periode sebelumnya
        var avgCurrent = summaries.Count > 0
            ? Math.Round(summaries.Average(s => s.PunctualityPercent), 1)
            : 0.0;
        var avgPrev = prevPercents.Count > 0
            ? Math.Round(prevPercents.Average(), 1)
            : 0.0;

        var adminDto = new PunctualityAdminDto
        {
            Period = period,
            Range = new DateRangeDto
            {
                From = from.ToString("yyyy-MM-dd"),
                To = to.ToString("yyyy-MM-dd")
            },
            WorkDays = summaries.Sum(s => s.WorkDays),
            OnTime = summaries.Sum(s => s.OnTime),
            Late = summaries.Sum(s => s.Late),
            Absent = summaries.Sum(s => s.Absent),
            Excused = summaries.Sum(s => s.Excused),
            PunctualityPercent = avgCurrent,
            PreviousPercent = avgPrev,
            Trend = PunctualityCalculator.ComputeTrend(avgCurrent, avgPrev),
            // Urutkan dari ketepatan waktu terendah (ascending)
            Users = summaries.OrderBy(s => s.PunctualityPercent).ToList(),
        };

        return Ok(ApiResponse<PunctualityAdminDto>.Ok(adminDto));
    }

    // ─── Helper: hitung punctuality untuk 1 user ─────────────────────────────
    private async Task<PunctualityDto> ComputeUserPunctualityDto(
        int targetUserId,
        string period,
        DateOnly from, DateOnly to,
        DateOnly prevFrom, DateOnly prevTo,
        HashSet<DayOfWeek> workDayNames,
        DateOnly today, TimeOnly nowTime, TimeOnly jamMasuk)
    {
        var minDate = prevFrom < from ? prevFrom : from;
        var maxDate = to > prevTo ? to : prevTo;

        var attendances = await _context.Attendances
            .Where(a => a.UserId == targetUserId && a.Date >= minDate && a.Date <= maxDate)
            .Select(a => new { a.Date, a.Status, a.CheckIn })
            .ToListAsync();

        var curAtt = attendances.Where(a => a.Date >= from && a.Date <= to).ToList();
        var prevAtt = attendances.Where(a => a.Date >= prevFrom && a.Date <= prevTo).ToList();

        // Aturan 3: Hari ini dihitung jika sudah check-in ATAU jam masuk sudah lewat
        var hasCheckedInToday = curAtt.Any(a => a.Date == today && a.CheckIn != null);
        var todayCounts = PunctualityCalculator.IsTodayElapsed(hasCheckedInToday, nowTime, jamMasuk);
        var elapsedWorkDays = PunctualityCalculator.CountElapsedWorkDays(from, to, workDayNames, today, todayCounts);

        var effectiveTo = to < today ? to : (todayCounts ? today : today.AddDays(-1));
        var validAtt = curAtt.Where(a => workDayNames.Contains(a.Date.DayOfWeek) && a.Date <= effectiveTo).ToList();

        var onTime = validAtt.Count(a => a.Status == "hadir");
        var late = validAtt.Count(a => a.Status == "telat");
        var excused = validAtt.Count(a => a.Status == "izin" || a.Status == "sakit" || a.Status == "cuti");
        var denominator = Math.Max(0, elapsedWorkDays - excused);
        var absent = Math.Max(0, denominator - onTime - late);
        var pct = PunctualityCalculator.ComputePunctualityPercent(onTime, elapsedWorkDays, excused);

        // Periode sebelumnya
        var prevElapsed = PunctualityCalculator.CountElapsedWorkDays(prevFrom, prevTo, workDayNames, today, true);
        var prevValidAtt = prevAtt.Where(a => workDayNames.Contains(a.Date.DayOfWeek)).ToList();
        var prevOnTime = prevValidAtt.Count(a => a.Status == "hadir");
        var prevExcused = prevValidAtt.Count(a => a.Status == "izin" || a.Status == "sakit" || a.Status == "cuti");
        var prevPct = PunctualityCalculator.ComputePunctualityPercent(prevOnTime, prevElapsed, prevExcused);

        var trend = PunctualityCalculator.ComputeTrend(pct, prevPct);

        return new PunctualityDto
        {
            Period = period,
            Range = new DateRangeDto
            {
                From = from.ToString("yyyy-MM-dd"),
                To = to.ToString("yyyy-MM-dd")
            },
            WorkDays = denominator,
            OnTime = onTime,
            Late = late,
            Absent = absent,
            Excused = excused,
            PunctualityPercent = pct,
            PreviousPercent = prevPct,
            Trend = trend,
        };
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
}
