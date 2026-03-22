namespace SmartAttendanceApi.DTOs;

// Auth DTOs
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = "user";
}

public class UpdateProfileRequest
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string? PhotoProfile { get; set; } // Base64 image
    public string? Role { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string? PhotoProfile { get; set; }
    public bool IsActive { get; set; }
}

// Attendance DTOs
public class CheckInRequest
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LocationName { get; set; }
    public string? Photo { get; set; }
    public string? Notes { get; set; }
}

public class CheckOutRequest
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LocationName { get; set; }
    public string? Photo { get; set; }
}

public class AttendanceDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string Date { get; set; } = string.Empty;
    public string? CheckIn { get; set; }
    public string? CheckOut { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? LocationIn { get; set; }
    public string? LocationOut { get; set; }
    public double? LatitudeIn { get; set; }
    public double? LongitudeIn { get; set; }
    public string? PhotoIn { get; set; }
    public string? PhotoOut { get; set; }
    public string? Notes { get; set; }
}

// Leave DTOs
public class LeaveRequestDto
{
    public string Type { get; set; } = "izin";
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Attachment { get; set; }
}

public class LeaveApprovalDto
{
    public string Status { get; set; } = string.Empty; // approved / rejected
    public string? AdminNotes { get; set; }
}

public class LeaveResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string Type { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Attachment { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ApproverName { get; set; }
    public string? AdminNotes { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

// Dashboard DTOs
public class DashboardUserDto
{
    public string TodayStatus { get; set; } = "belum_absen";
    public string? LastCheckIn { get; set; }
    public string? LastCheckOut { get; set; }
    public int TotalHadir { get; set; }
    public int TotalTelat { get; set; }
    public int TotalIzin { get; set; }
    public int TotalAlpha { get; set; }
}

public class DashboardAdminDto
{
    public int TotalKaryawan { get; set; }
    public int HadirHariIni { get; set; }
    public int TelatHariIni { get; set; }
    public int TidakHadirHariIni { get; set; }
    public int IzinHariIni { get; set; }
    public int PendingLeave { get; set; }
    public List<AttendanceDto> RecentAttendances { get; set; } = new();
}

// API Response wrapper
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message)
        => new() { Success = false, Message = message };
}

public class PaginatedResponse<T>
{
    public List<T> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
