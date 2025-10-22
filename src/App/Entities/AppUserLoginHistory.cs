using System.ComponentModel.DataAnnotations;
using Diiwo.Core.Domain.Entities;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.App.Entities;

/// <summary>
/// APP ARCHITECTURE - Login history tracking
/// Tracks all login attempts for security audit
/// </summary>
public class AppUserLoginHistory : DomainEntity
{
    public AppUserLoginHistory()
    {
    }

    /// <summary>
    /// Foreign key to the user who attempted to login
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Whether the login attempt was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Authentication method used for the login attempt
    /// </summary>
    public AuthMethod AuthMethod { get; set; } = AuthMethod.EmailPassword;

    /// <summary>
    /// IP address from which the login attempt was made
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string of the client that made the login attempt
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Reason for login failure (null if successful)
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Timestamp when the login attempt occurred
    /// </summary>
    public DateTime LoginAttemptAt { get; set; } = DateTime.UtcNow;

    // Note: Audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) and IsActive come from DomainEntity

    /// <summary>
    /// Navigation property to the user who made the login attempt
    /// </summary>
    public virtual AppUser User { get; set; } = null!;

    /// <summary>
    /// Create successful login record
    /// </summary>
    /// <param name="userId">ID of the user who successfully logged in</param>
    /// <param name="authMethod">Authentication method used</param>
    /// <param name="ipAddress">IP address of the client</param>
    /// <param name="userAgent">User agent string of the client</param>
    /// <returns>New successful login history record</returns>
    public static AppUserLoginHistory CreateSuccessful(Guid userId, AuthMethod authMethod, string? ipAddress = null, string? userAgent = null)
    {
        return new AppUserLoginHistory
        {
            UserId = userId,
            IsSuccessful = true,
            AuthMethod = authMethod,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }

    /// <summary>
    /// Create failed login record
    /// </summary>
    /// <param name="userId">ID of the user who attempted to login</param>
    /// <param name="authMethod">Authentication method used</param>
    /// <param name="failureReason">Reason why the login failed</param>
    /// <param name="ipAddress">IP address of the client</param>
    /// <param name="userAgent">User agent string of the client</param>
    /// <returns>New failed login history record</returns>
    public static AppUserLoginHistory CreateFailed(Guid userId, AuthMethod authMethod, string failureReason, string? ipAddress = null, string? userAgent = null)
    {
        return new AppUserLoginHistory
        {
            UserId = userId,
            IsSuccessful = false,
            AuthMethod = authMethod,
            FailureReason = failureReason,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }
}