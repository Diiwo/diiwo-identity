using Diiwo.Identity.Shared.Attributes;
using Diiwo.Identity.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace MedicalSystem.Entities;

/// <summary>
/// Example medical appointment entity showing how to use PermissionAttribute
/// This demonstrates automatic permission generation for domain-specific actions
/// </summary>
[Permission("View", "View appointment details and schedule")]
[Permission("Create", "Schedule new appointments")]
[Permission("Update", "Modify existing appointments")]
[Permission("Cancel", "Cancel scheduled appointments")]
[Permission("Reschedule", "Move appointments to different times")]
[Permission("ViewHistory", "Access appointment history", PermissionScope.Object)]
[Permission("ManageWaitlist", "Manage appointment waiting lists", PermissionScope.Global)]
public class Appointment
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(200)]
    public required string Title { get; set; }

    public string? Description { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; }

    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(30);

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    [Required]
    public Guid PatientId { get; set; }

    [Required] 
    public Guid DoctorId { get; set; }

    public Guid? RoomId { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties (would be configured in DbContext)
    // public virtual Patient Patient { get; set; }
    // public virtual Doctor Doctor { get; set; }
    // public virtual Room? Room { get; set; }
}

public enum AppointmentStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled,
    NoShow,
    Rescheduled
}