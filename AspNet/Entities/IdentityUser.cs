using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
/// ASPNET ARCHITECTURE - Enterprise user entity that extends IdentityUser
/// Combines ASP.NET Core Identity with enterprise functionality
/// </summary>
public class IdentityUser : IdentityUser<Guid>
{
    public IdentityUser()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Enterprise audit properties
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

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
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Get user's display name (full name or email)
    /// </summary>
    public string DisplayName => !string.IsNullOrEmpty(FullName) ? FullName : Email ?? UserName ?? "Unknown";

    /// <summary>
    /// Check if user can login (combines Identity + enterprise checks)
    /// </summary>
    public bool CanLogin => !LockoutEnabled && EmailConfirmed;

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
}