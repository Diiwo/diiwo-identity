using Diiwo.Identity.Shared.Attributes;
using Diiwo.Identity.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace MedicalSystem.Entities;

/// <summary>
/// Example patient entity showing different permission scopes and priorities
/// Demonstrates how to define medical domain-specific permissions
/// </summary>
[Permission("View", "View basic patient information", PermissionScope.Model, 10)]
[Permission("Create", "Register new patients", PermissionScope.Global, 20)]
[Permission("Edit", "Modify patient information", PermissionScope.Object, 15)]
[Permission("Archive", "Archive inactive patients", PermissionScope.Global, 30)]
[Permission("ViewMedicalHistory", "Access complete medical history", PermissionScope.Object, 50)]
[Permission("ViewSensitiveInfo", "Access sensitive patient data", PermissionScope.Object, 100)]
[Permission("ExportData", "Export patient data", PermissionScope.Global, 40)]
public class Patient
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [StringLength(100)]
    public required string LastName { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public required string Email { get; set; }

    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [Required]
    public DateTime DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? InsuranceNumber { get; set; }

    [StringLength(20)]
    public string? SocialSecurityNumber { get; set; } // Sensitive data

    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Computed properties
    public string FullName => $"{FirstName} {LastName}";
    public int Age => DateTime.Today.Year - DateOfBirth.Year - (DateTime.Today.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);

    // Navigation properties
    // public virtual ICollection<Appointment> Appointments { get; set; }
    // public virtual ICollection<MedicalRecord> MedicalRecords { get; set; }
}

public enum Gender
{
    Male,
    Female,
    Other,
    PreferNotToSay
}