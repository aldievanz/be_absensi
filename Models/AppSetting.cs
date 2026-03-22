using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartAttendanceApi.Models;

[Table("app_settings")]
public class AppSetting
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("value")]
    public string Value { get; set; } = string.Empty;

    [StringLength(255)]
    [Column("description")]
    public string? Description { get; set; }
}
