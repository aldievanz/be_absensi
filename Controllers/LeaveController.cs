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
public class LeaveController : ControllerBase
{
    private readonly AppDbContext _context;

    public LeaveController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>POST api/leave - Ajukan izin/cuti</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LeaveRequestDto request)
    {
        var userId = GetUserId();

        var leave = new LeaveRequest
        {
            UserId = userId,
            Type = request.Type,
            StartDate = DateOnly.Parse(request.StartDate),
            EndDate = DateOnly.Parse(request.EndDate),
            Reason = request.Reason,
            Attachment = request.Attachment,
            Status = "pending"
        };

        _context.LeaveRequests.Add(leave);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { id = leave.Id }, "Pengajuan berhasil dikirim"));
    }

    /// <summary>GET api/leave - Riwayat izin user</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyLeaves()
    {
        var userId = GetUserId();

        var leaves = await _context.LeaveRequests
            .Include(l => l.User)
            .Include(l => l.Approver)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<List<LeaveResponseDto>>.Ok(
            leaves.Select(MapToDto).ToList()));
    }

    /// <summary>GET api/leave/all - Semua pengajuan (Admin)</summary>
    [HttpGet("all")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var query = _context.LeaveRequests
            .Include(l => l.User)
            .Include(l => l.Approver)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(l => l.Status == status);

        var leaves = await query
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<List<LeaveResponseDto>>.Ok(
            leaves.Select(MapToDto).ToList()));
    }

    /// <summary>PUT api/leave/{id}/approve - Approve/Reject (Admin)</summary>
    [HttpPut("{id}/approve")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Approve(int id, [FromBody] LeaveApprovalDto request)
    {
        var leave = await _context.LeaveRequests.FindAsync(id);
        if (leave == null)
            return NotFound(ApiResponse<string>.Fail("Pengajuan tidak ditemukan"));

        if (leave.Status != "pending")
            return BadRequest(ApiResponse<string>.Fail("Pengajuan sudah diproses"));

        leave.Status = request.Status;
        leave.AdminNotes = request.AdminNotes;
        leave.ApprovedBy = GetUserId();
        leave.ApprovedAt = DateTime.UtcNow;
        leave.UpdatedAt = DateTime.UtcNow;

        // Jika disetujui, buat record attendance untuk tanggal izin
        if (request.Status == "approved")
        {
            var startDate = leave.StartDate;
            var endDate = leave.EndDate;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var existing = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.UserId == leave.UserId && a.Date == date);

                if (existing == null)
                {
                    _context.Attendances.Add(new Attendance
                    {
                        UserId = leave.UserId,
                        Date = date,
                        Status = leave.Type, // izin/sakit/cuti
                        Notes = $"Izin: {leave.Reason}"
                    });
                }
            }
        }

        await _context.SaveChangesAsync();

        var statusText = request.Status == "approved" ? "disetujui" : "ditolak";
        return Ok(ApiResponse<object>.Ok(new { id = leave.Id }, $"Pengajuan {statusText}"));
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    private static LeaveResponseDto MapToDto(LeaveRequest l) => new()
    {
        Id = l.Id,
        UserId = l.UserId,
        UserName = l.User?.Name,
        Type = l.Type,
        StartDate = l.StartDate.ToString("yyyy-MM-dd"),
        EndDate = l.EndDate.ToString("yyyy-MM-dd"),
        Reason = l.Reason,
        Attachment = l.Attachment,
        Status = l.Status,
        ApproverName = l.Approver?.Name,
        AdminNotes = l.AdminNotes,
        CreatedAt = l.CreatedAt.ToString("yyyy-MM-dd HH:mm")
    };
}
