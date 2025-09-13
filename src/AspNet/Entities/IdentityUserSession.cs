using System.ComponentModel.DataAnnotations;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
/// ASPNET ARCHITECTURE - Enterprise user session tracking
/// Tracks user sessions for security and audit purposes with enterprise features
/// </summary>
public class IdentityUserSession
{
    public IdentityUserSession()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        SessionToken = Guid.NewGuid().ToString("N");
        ExpiresAt = DateTime.UtcNow.AddHours(24); // Default 24 hours
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

    public bool IsActive { get; set; } = true;

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

    // Enterprise audit properties
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public virtual IdentityUser User { get; set; } = null!;

    /// <summary>
    /// Check if session is expired
    /// </summary>
    public bool IsExpired => ExpiresAt <= DateTime.UtcNow;

    /// <summary>
    /// Check if session is valid (active, not expired)
    /// </summary>
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
}