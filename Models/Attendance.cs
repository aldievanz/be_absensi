using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartAttendanceApi.Models;

[Table("attendances")]
public class Attendance
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("date")]
    public DateOnly Date { get; set; }

    [Column("check_in")]
    public TimeOnly? CheckIn { get; set; }

    [Column("check_out")]
    public TimeOnly? CheckOut { get; set; }

    [Required]
    [StringLength(20)]
    [Column("status")]
    public string Status { get; set; } = "hadir"; // hadir, telat, izin, alpha

    [StringLength(255)]
    [Column("location_in")]
    public string? LocationIn { get; set; }

    [Column("latitude_in")]
    public double? LatitudeIn { get; set; }

    [Column("longitude_in")]
    public double? LongitudeIn { get; set; }

    [StringLength(255)]
    [Column("location_out")]
    public string? LocationOut { get; set; }

    [Column("latitude_out")]
    public double? LatitudeOut { get; set; }

    [Column("longitude_out")]
    public double? LongitudeOut { get; set; }

    [Column("photo_in")]
    public string? PhotoIn { get; set; }

    [Column("photo_out")]
    public string? PhotoOut { get; set; }

    [StringLength(500)]
    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("UserId")]
    public User? User { get; set; }
}
