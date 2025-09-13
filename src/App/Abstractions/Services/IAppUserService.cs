using Diiwo.Identity.App.Entities;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.App.Abstractions.Services;

/// <summary>
/// App user service interface for App architecture
/// Simple user management without ASP.NET Core Identity complexity
/// Provides core user operations for authentication and user management
/// </summary>
public interface IAppUserService
{
    /// <summary>
    /// Retrieves a user by their unique identifier
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<AppUser?> GetUserByIdAsync(Guid userId);
    
    /// <summary>
    /// Retrieves a user by their email address
    /// </summary>
    /// <param name="email">The email address of the user</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<AppUser?> GetUserByEmailAsync(string email);
    
    /// <summary>
    /// Retrieves a user by their username
    /// </summary>
    /// <param name="username">The username of the user</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<AppUser?> GetUserByUsernameAsync(string username);

    /// <summary>
    /// Creates a new user account
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="passwordHash">The hashed password</param>
    /// <param name="firstName">The user's first name (optional)</param>
    /// <param name="lastName">The user's last name (optional)</param>
    /// <returns>The created user</returns>
    Task<AppUser> CreateUserAsync(string email, string passwordHash, string? firstName = null, string? lastName = null);
    
    /// <summary>
    /// Updates an existing user's information
    /// </summary>
    /// <param name="user">The user to update</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> UpdateUserAsync(AppUser user);
    
    /// <summary>
    /// Deletes a user account
    /// </summary>
    /// <param name="userId">The ID of the user to delete</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> DeleteUserAsync(Guid userId);

    /// <summary>
    /// Validates a user's password against the stored hash
    /// </summary>
    /// <param name="user">The user to validate</param>
    /// <param name="password">The plain text password to check</param>
    /// <returns>True if password is correct, false otherwise</returns>
    Task<bool> ValidatePasswordAsync(AppUser user, string password);
    
    /// <summary>
    /// Changes a user's password after validating their current password
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="currentPassword">The user's current password</param>
    /// <param name="newPassword">The new password to set</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

    /// <summary>
    /// Confirms a user's email address using a confirmation token
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="token">The email confirmation token</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> ConfirmEmailAsync(Guid userId, string token);
    
    /// <summary>
    /// Generates an email confirmation token for a user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <returns>The confirmation token</returns>
    Task<string> GenerateEmailConfirmationTokenAsync(Guid userId);

    /// <summary>
    /// Generates a password reset token for a user by email
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <returns>The reset token if user exists, null otherwise</returns>
    Task<string?> GeneratePasswordResetTokenAsync(string email);
    
    /// <summary>
    /// Resets a user's password using a reset token
    /// </summary>
    /// <param name="token">The password reset token</param>
    /// <param name="newPassword">The new password to set</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> ResetPasswordAsync(string token, string newPassword);

    /// <summary>
    /// Creates a new user session
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="sessionToken">The session token</param>
    /// <param name="ipAddress">The client's IP address</param>
    /// <param name="userAgent">The client's user agent</param>
    /// <param name="sessionType">The type of session</param>
    /// <returns>The created session</returns>
    Task<AppUserSession> CreateSessionAsync(Guid userId, string sessionToken, string? ipAddress = null, string? userAgent = null, SessionType sessionType = SessionType.Web);
    
    /// <summary>
    /// Validates if a session token is active and not expired
    /// </summary>
    /// <param name="sessionToken">The session token to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    Task<bool> ValidateSessionAsync(string sessionToken);
    
    /// <summary>
    /// Revokes a specific user session
    /// </summary>
    /// <param name="sessionToken">The session token to revoke</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> RevokeSessionAsync(string sessionToken);
    
    /// <summary>
    /// Revokes all active sessions for a user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    Task RevokeAllUserSessionsAsync(Guid userId);

    /// <summary>
    /// Logs a user login attempt for security auditing
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="isSuccessful">Whether the login was successful</param>
    /// <param name="ipAddress">The client's IP address</param>
    /// <param name="userAgent">The client's user agent</param>
    /// <param name="failureReason">The reason for failure (if unsuccessful)</param>
    /// <param name="authMethod">The authentication method used</param>
    Task LogLoginAttemptAsync(Guid userId, bool isSuccessful, string? ipAddress = null, string? userAgent = null, string? failureReason = null, AuthMethod authMethod = AuthMethod.Password);
    
    /// <summary>
    /// Gets the login history for a user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="take">The number of records to retrieve</param>
    /// <returns>List of login history records</returns>
    Task<List<AppUserLoginHistory>> GetLoginHistoryAsync(Guid userId, int take = 50);
}