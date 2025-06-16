using System.ComponentModel.DataAnnotations;
using Diiwo.Core.Domain.Entities;

namespace Diiwo.Identity.App.Entities;

/// <summary>
/// APP ARCHITECTURE - Simple user entity
/// Base user entity for authentication and authorization
/// Independent implementation that doesn't require ASP.NET Core Identity
/// Optimized for simplicity and direct database access
/// </summary>
public class AppUser : UserTrackedEntity
{
    [Required]
    [StringLength(150)]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [StringLength(255)]
    public required string PasswordHash { get; set; }

    [StringLength(50)]
    public string? FirstName { get; set; }

    [StringLength(50)]
    public string? LastName { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(150)]
    public string? Username { get; set; }

    public string? ProfileImageUrl { get; set; }

    public bool EmailConfirmed { get; set; } = false;

    public bool PhoneConfirmed { get; set; } = false;

    public bool TwoFactorEnabled { get; set; } = false;
    
    public bool IsTwoFactorEnabled => TwoFactorEnabled;

    public DateTime? LastLoginAt { get; set; }

    public string? LastLoginIp { get; set; }

    public int FailedLoginAttempts { get; set; } = 0;

    public DateTime? LockedUntil { get; set; }

    public string? EmailConfirmationToken { get; set; }

    public DateTime? EmailConfirmationTokenExpiry { get; set; }

    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpires { get; set; }

    public string? TwoFactorSecret { get; set; }

    // Navigation properties
    public virtual ICollection<AppUserSession> UserSessions { get; set; } = new List<AppUserSession>();
    public virtual ICollection<AppUserLoginHistory> LoginHistory { get; set; } = new List<AppUserLoginHistory>();
    public virtual ICollection<AppUserPermission> UserPermissions { get; set; } = new List<AppUserPermission>();
    public virtual ICollection<AppGroup> UserGroups { get; set; } = new List<AppGroup>();

    /// <summary>
    /// Get user's full name
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Get user's display name (full name or email)
    /// </summary>
    public string DisplayName => !string.IsNullOrEmpty(FullName) ? FullName : Email;

    /// <summary>
    /// Check if user account is locked (combined with Core's IsLocked property)
    /// </summary>
    public bool IsAccountLocked => IsLocked || (LockedUntil.HasValue && LockedUntil > DateTime.UtcNow);

    /// <summary>
    /// Check if user can login (uses Core's IsActive property)
    /// </summary>
    public bool CanLogin => IsActive && !IsAccountLocked && EmailConfirmed;
}