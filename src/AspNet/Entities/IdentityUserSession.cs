using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Diiwo.Identity.Shared.Enums;
using Diiwo.Core.Domain.Interfaces;
using Diiwo.Core.Domain.Enums;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
/// ASPNET ARCHITECTURE - Enterprise user session tracking
/// Tracks user sessions for security and audit purposes with enterprise features
/// </summary>
public class IdentityUserSession : IDomainEntity
{
    public IdentityUserSession()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        SessionToken = Guid.NewGuid().ToString("N");
        ExpiresAt = DateTime.UtcNow.AddHours(24); // Default 24 hours
        State = EntityState.Active;
    }

    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [Required]
    [StringLength(255)]
    public required string SessionToken { get; set; }

    public SessionType SessionType { get; set; } = SessionType.Web;

    public DateTime ExpiresAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    [NotMapped]
    public bool IsActive => State == EntityState.Active;

    public DateTime? LastActivityAt { get; set; }

    // Enterprise features
    public string? DeviceFingerprint { get; set; }

    public string? Location { get; set; }

    /// <summary>
    /// JWT Refresh token for token-based authentication
    /// </summary>
    [StringLength(500)]
    public string? RefreshToken { get; set; }

    public bool IsSSO { get; set; } = false;

    public string? SSOProvider { get; set; }

    // IUserTracked implementation - allows automatic audit via AuditInterceptor
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public EntityState State { get; set; } = EntityState.Active;

    // Navigation properties
    public virtual IdentityUser User { get; set; } = null!;

    /// <summary>
    /// Check if session is expired
    /// </summary>
    [NotMapped]
    public bool IsExpired => ExpiresAt <= DateTime.UtcNow;

    /// <summary>
    /// Check if session is valid (active, not expired)
    /// </summary>
    [NotMapped]
    public bool IsValid => IsActive && !IsExpired;

    /// <summary>
    /// Update last activity with enterprise tracking
    /// </summary>
    public void UpdateLastActivity(string? location = null, string? deviceFingerprint = null)
    {
        LastActivityAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(location))
        {
            Location = location;
        }
        if (!string.IsNullOrEmpty(deviceFingerprint))
        {
            DeviceFingerprint = deviceFingerprint;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark as SSO session
    /// </summary>
    public void MarkAsSSO(string provider)
    {
        IsSSO = true;
        SSOProvider = provider;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update refresh token for JWT authentication
    /// </summary>
    public void SetRefreshToken(string refreshToken)
    {
        RefreshToken = refreshToken;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Clear refresh token (logout, security, etc.)
    /// </summary>
    public void ClearRefreshToken()
    {
        RefreshToken = null;
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