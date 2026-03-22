using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartAttendanceApi.Models;

[Table("leave_requests")]
public class LeaveRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [StringLength(20)]
    [Column("type")]
    public string Type { get; set; } = "izin"; // izin, sakit, cuti

    [Required]
    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Required]
    [Column("end_date")]
    public DateOnly EndDate { get; set; }

    [Required]
    [StringLength(500)]
    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [StringLength(500)]
    [Column("attachment")]
    public string? Attachment { get; set; }

    [Required]
    [StringLength(20)]
    [Column("status")]
    public string Status { get; set; } = "pending"; // pending, approved, rejected

    [Column("approved_by")]
    public int? ApprovedBy { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [StringLength(500)]
    [Column("admin_notes")]
    public string? AdminNotes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("ApprovedBy")]
    public User? Approver { get; set; }
}
