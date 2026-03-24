using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAttendanceApi.Data;
using SmartAttendanceApi.DTOs;
using SmartAttendanceApi.Models;

namespace SmartAttendanceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Semua user boleh baca setting
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SettingsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>GET api/settings</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var settings = await _context.AppSettings.ToListAsync();
        return Ok(ApiResponse<List<AppSetting>>.Ok(settings));
    }

    /// <summary>PUT api/settings</summary>
    [HttpPut]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update([FromBody] Dictionary<string, string> settings)
    {
        foreach (var kvp in settings)
        {
            var setting = await _context.AppSettings
                .FirstOrDefaultAsync(s => s.Key == kvp.Key);

            if (setting != null)
            {
                setting.Value = kvp.Value;
            }
            else
            {
                _context.AppSettings.Add(new AppSetting
                {
                    Key = kvp.Key,
                    Value = kvp.Value
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("updated", "Pengaturan berhasil disimpan"));
    }

    /// <summary>DELETE api/settings/reset-data</summary>
    [HttpDelete("reset-data")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ResetData()
    {
        // Use raw SQL to swiftly clear tables
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM attendances; ALTER TABLE attendances AUTO_INCREMENT = 1;");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM leave_requests; ALTER TABLE leave_requests AUTO_INCREMENT = 1;");
        return Ok(ApiResponse<string>.Ok("cleared", "Semua data Riwayat Absensi & Izin berhasil direset bersih (0 data)!"));
    }
}
