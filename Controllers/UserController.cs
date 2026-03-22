using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAttendanceApi.Data;
using SmartAttendanceApi.DTOs;
using SmartAttendanceApi.Models;

namespace SmartAttendanceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>GET api/user</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Users
            .OrderBy(u => u.Name)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
                Position = u.Position,
                Department = u.Department,
                Phone = u.Phone,
                PhotoProfile = u.PhotoProfile,
                IsActive = u.IsActive
            })
            .ToListAsync();

        return Ok(ApiResponse<List<UserDto>>.Ok(users));
    }

    /// <summary>POST api/user/search</summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] object queryObj)
    {
        var queryStr = queryObj?.ToString() ?? "";

        var users = await _context.Users
            .Where(u => u.Name.Contains(queryStr) ||
                        u.Email.Contains(queryStr) ||
                        (u.Department != null && u.Department.Contains(queryStr)))
            .OrderBy(u => u.Name)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
                Position = u.Position,
                Department = u.Department,
                Phone = u.Phone,
                PhotoProfile = u.PhotoProfile,
                IsActive = u.IsActive
            })
            .ToListAsync();

        return Ok(new { results = users });
    }

    /// <summary>GET api/user/{id}</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(ApiResponse<string>.Fail("User tidak ditemukan"));

        return Ok(ApiResponse<UserDto>.Ok(new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Position = user.Position,
            Department = user.Department,
            Phone = user.Phone,
            PhotoProfile = user.PhotoProfile,
            IsActive = user.IsActive
        }));
    }

    /// <summary>PUT api/user/{id}</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] RegisterRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(ApiResponse<string>.Fail("User tidak ditemukan"));

        user.Name = request.Name;
        user.Email = request.Email;
        user.Role = request.Role;
        user.Position = request.Position;
        user.Department = request.Department;
        user.Phone = request.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Password))
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok("updated", "User berhasil diupdate"));
    }

    /// <summary>PUT api/user/{id}/toggle-active</summary>
    [HttpPut("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(ApiResponse<string>.Fail("User tidak ditemukan"));

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var status = user.IsActive ? "diaktifkan" : "dinonaktifkan";
        return Ok(ApiResponse<string>.Ok(status, $"User berhasil {status}"));
    }

    /// <summary>DELETE api/user/{id}</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(ApiResponse<string>.Fail("User tidak ditemukan"));

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok("deleted", "User berhasil dihapus"));
    }
}
