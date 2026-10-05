using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SmartAttendanceApi.Models;

[Table("users")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("password")]
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    [Column("role")]
    public string Role { get; set; } = "user"; // "admin" atau "user"

    [StringLength(100)]
    [Column("position")]
    public string? Position { get; set; }

    [StringLength(100)]
    [Column("department")]
    public string? Department { get; set; }

    [StringLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("photo_profile")]
    public string? PhotoProfile { get; set; }

    [Column("face_embedding")]
    public string? FaceEmbedding { get; set; } // JSON array of float (128-d face descriptor)

    [Column("face_registered_at")]
    public DateTime? FaceRegisteredAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [JsonIgnore]
    public ICollection<Attendance>? Attendances { get; set; }
    [JsonIgnore]
    public ICollection<LeaveRequest>? LeaveRequests { get; set; }
}
