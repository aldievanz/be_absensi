using Microsoft.EntityFrameworkCore;
using SmartAttendanceApi.Models;

namespace SmartAttendanceApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User unique constraints
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Attendance: 1 user = 1 record per hari
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Attendances)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // LeaveRequest relations
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasOne(e => e.User)
                  .WithMany(u => u.LeaveRequests)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Approver)
                  .WithMany()
                  .HasForeignKey(e => e.ApprovedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // AppSettings unique key
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasIndex(e => e.Key).IsUnique();
        });
    }
}
