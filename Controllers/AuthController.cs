using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SmartAttendanceApi.Data;
using SmartAttendanceApi.DTOs;
using SmartAttendanceApi.Models;

namespace SmartAttendanceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>POST api/auth/login</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            return Unauthorized(ApiResponse<string>.Fail("Email atau password salah"));

        if (!user.IsActive)
            return Unauthorized(ApiResponse<string>.Fail("Akun sudah dinonaktifkan"));

        var token = GenerateJwtToken(user);

        var response = new LoginResponse
        {
            Token = token,
            User = MapToUserDto(user)
        };

        return Ok(ApiResponse<LoginResponse>.Ok(response, "Login berhasil"));
    }

    /// <summary>POST api/auth/register</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest(ApiResponse<string>.Fail("Email sudah terdaftar"));

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            Position = request.Position,
            Department = request.Department,
            Phone = request.Phone
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserDto>.Ok(MapToUserDto(user), "Registrasi berhasil"));
    }

    /// <summary>GET api/auth/me</summary>
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetUserIdFromToken();
        var user = await _context.Users.FindAsync(userId);

        if (user == null) return NotFound(ApiResponse<string>.Fail("User tidak ditemukan"));

        return Ok(ApiResponse<UserDto>.Ok(MapToUserDto(user)));
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetUserIdFromToken()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : 0;
    }

    /// <summary>PUT api/auth/profile</summary>
    [HttpPut("profile")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserIdFromToken();
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(ApiResponse<string>.Fail("User tidak ditemukan"));

        if (!string.IsNullOrEmpty(request.Name)) user.Name = request.Name;
        if (!string.IsNullOrEmpty(request.Phone)) user.Phone = request.Phone;
        if (!string.IsNullOrEmpty(request.Position)) user.Position = request.Position;
        if (!string.IsNullOrEmpty(request.Department)) user.Department = request.Department;
        if (request.PhotoProfile != null) user.PhotoProfile = request.PhotoProfile;
        if (!string.IsNullOrEmpty(request.Role)) user.Role = request.Role;

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserDto>.Ok(MapToUserDto(user), "Profil berhasil diperbarui"));
    }

    private static UserDto MapToUserDto(User user) => new()
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
    };
}
