using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Diiwo.Core.Domain.Interfaces;
using Diiwo.Core.Domain.Enums;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
/// ASPNET ARCHITECTURE - Enterprise user entity that extends IdentityUser
/// Combines ASP.NET Core Identity with enterprise functionality
/// Implements IDomainEntity for consistency with App architecture
/// </summary>
public class IdentityUser : IdentityUser<Guid>, IDomainEntity
{
    public IdentityUser()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        State = EntityState.Active;
    }

    // IUserTracked implementation - allows automatic audit via AuditInterceptor
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public EntityState State { get; set; } = EntityState.Active;

    /// <summary>
    /// Whether the user is active (not soft deleted)
    /// </summary>
    [NotMapped]
    public bool IsActive => State == EntityState.Active;

    // Enhanced user properties
    [StringLength(50)]
    public string? FirstName { get; set; }

    [StringLength(50)]
    public string? LastName { get; set; }

    public string? ProfileImageUrl { get; set; }

    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }

    // Two-factor authentication
    public string? TwoFactorSecret { get; set; }

    // Enterprise extensibility (JSON for flexibility)
    public string? ExtendedProperties { get; set; }

    // Optional foreign key to App architecture (configured via ForeignKeyConfiguration)
    public Guid? AppUserId { get; set; }

    // Navigation properties
    public virtual ICollection<IdentityUserSession> IdentityUserSessions { get; set; } = new List<IdentityUserSession>();
    public virtual ICollection<IdentityLoginHistory> IdentityLoginHistory { get; set; } = new List<IdentityLoginHistory>();
    public virtual ICollection<IdentityUserPermission> IdentityUserPermissions { get; set; } = new List<IdentityUserPermission>();
    public virtual ICollection<IdentityGroup> IdentityUserGroups { get; set; } = new List<IdentityGroup>();

    // Optional navigation to App architecture (when foreign key is configured)
    public virtual Diiwo.Identity.App.Entities.AppUser? AppUser { get; set; }

    /// <summary>
    /// Get user's full name
    /// </summary>
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Get user's display name (full name or email)
    /// </summary>
    [NotMapped]
    public string DisplayName => !string.IsNullOrEmpty(FullName) ? FullName : Email ?? UserName ?? "Unknown";

    /// <summary>
    /// Check if user can login (combines Identity + enterprise checks)
    /// </summary>
    [NotMapped]
    public bool CanLogin => !LockoutEnabled && EmailConfirmed && IsActive;

    /// <summary>
    /// Record successful login with enterprise tracking
    /// </summary>
    public virtual void RecordSuccessfulLogin(string? ipAddress = null)
    {
        LastLoginAt = DateTime.UtcNow;
        LastLoginIp = ipAddress;
        AccessFailedCount = 0; // Reset Identity failed count
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// IDomainEntity implementation - Soft delete the entity
    /// </summary>
    public void SoftDelete()
    {
        State = EntityState.Terminated;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// IDomainEntity implementation - Restore a soft-deleted entity
    /// </summary>
    public void Restore()
    {
        State = EntityState.Active;
        UpdatedAt = DateTime.UtcNow;
    }
}