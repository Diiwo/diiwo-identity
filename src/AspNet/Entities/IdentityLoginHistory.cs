using System.ComponentModel.DataAnnotations;
using Diiwo.Identity.Shared.Enums;
using Diiwo.Core.Domain.Interfaces;
using Diiwo.Core.Domain.Enums;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
/// ASPNET ARCHITECTURE - Enterprise login history tracking
/// Tracks all login attempts for security audit with enterprise features
/// </summary>
public class IdentityLoginHistory : IDomainEntity
{
    public IdentityLoginHistory()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        State = EntityState.Active;
    }

    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public bool IsSuccessful { get; set; }

    public AuthMethod AuthMethod { get; set; } = AuthMethod.EmailPassword;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? FailureReason { get; set; }

    public DateTime LoginAttemptAt { get; set; } = DateTime.UtcNow;

    // Enterprise features
    public string? DeviceFingerprint { get; set; }

    public string? Location { get; set; }

    public string? SSOProvider { get; set; }

    public int RiskScore { get; set; } = 0; // 0-100 risk assessment

    public bool RequiredMFA { get; set; } = false;

    public bool MFACompleted { get; set; } = false;

    // IUserTracked implementation - allows automatic audit via AuditInterceptor
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public EntityState State { get; set; } = EntityState.Active;

    // Navigation properties
    public virtual IdentityUser User { get; set; } = null!;

    /// <summary>
    /// Create successful login record with enterprise features
    /// </summary>
    public static IdentityLoginHistory CreateSuccessful(
        Guid userId, 
        AuthMethod authMethod, 
        string? ipAddress = null, 
        string? userAgent = null,
        string? deviceFingerprint = null,
        string? location = null,
        string? ssoProvider = null,
        int riskScore = 0)
    {
        return new IdentityLoginHistory
        {
            UserId = userId,
            IsSuccessful = true,
            AuthMethod = authMethod,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceFingerprint = deviceFingerprint,
            Location = location,
            SSOProvider = ssoProvider,
            RiskScore = riskScore
        };
    }

    /// <summary>
    /// Create failed login record with enterprise features
    /// </summary>
    public static IdentityLoginHistory CreateFailed(
        Guid userId, 
        AuthMethod authMethod, 
        string failureReason, 
        string? ipAddress = null, 
        string? userAgent = null,
        string? deviceFingerprint = null,
        string? location = null,
        int riskScore = 0)
    {
        return new IdentityLoginHistory
        {
            UserId = userId,
            IsSuccessful = false,
            AuthMethod = authMethod,
            FailureReason = failureReason,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceFingerprint = deviceFingerprint,
            Location = location,
            RiskScore = riskScore
        };
    }

    /// <summary>
    /// Calculate risk score based on various factors
    /// </summary>
    public void CalculateRiskScore()
    {
        int score = 0;

        // Unknown IP address
        if (string.IsNullOrEmpty(IpAddress))
            score += 20;

        // Failed login
        if (!IsSuccessful)
            score += 30;

        // Unknown device
        if (string.IsNullOrEmpty(DeviceFingerprint))
            score += 15;

        // Unknown location
        if (string.IsNullOrEmpty(Location))
            score += 10;

        // High-risk auth methods
        if (AuthMethod == AuthMethod.OAuth && string.IsNullOrEmpty(SSOProvider))
            score += 25;

        RiskScore = Math.Min(100, score);
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