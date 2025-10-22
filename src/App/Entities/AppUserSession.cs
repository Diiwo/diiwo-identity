using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Diiwo.Core.Domain.Entities;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.App.Entities;

/// <summary>
/// APP ARCHITECTURE - User session tracking
/// Tracks user sessions for security and audit purposes
/// </summary>
public class AppUserSession : DomainEntity
{
    public AppUserSession()
    {
        SessionToken = Guid.NewGuid().ToString("N");
        ExpiresAt = DateTime.UtcNow.AddHours(24); // Default 24 hours
    }

    /// <summary>
    /// Foreign key to the user who owns this session
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Unique token used to authenticate the session
    /// </summary>
    [Required]
    [StringLength(255)]
    public required string SessionToken { get; set; }

    /// <summary>
    /// Type of session (Web, Mobile, API, Desktop)
    /// </summary>
    public SessionType SessionType { get; set; } = SessionType.Web;

    /// <summary>
    /// When the session expires and becomes invalid
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// IP address from which the session was created
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string of the client that created the session
    /// </summary>
    public string? UserAgent { get; set; }

    // Note: IsActive property comes from DomainEntity

    /// <summary>
    /// Timestamp of the last activity on this session
    /// </summary>
    public DateTime? LastActivityAt { get; set; }

    // Note: Audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) come from DomainEntity

    /// <summary>
    /// Navigation property to the user who owns this session
    /// </summary>
    public virtual AppUser User { get; set; } = null!;

    /// <summary>
    /// Check if session is expired
    /// </summary>
    [NotMapped]
    public bool IsExpired => ExpiresAt <= DateTime.UtcNow;

    /// <summary>
    /// Check if session is valid (active and not expired)
    /// </summary>
    [NotMapped]
    public bool IsValid => IsActive && !IsExpired;
}