using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.AspNet.Abstractions.Services;

/// <summary>
/// AspNet user service interface for AspNet architecture
/// Enterprise user management with full ASP.NET Core Identity integration
/// Provides comprehensive user operations including authentication, authorization, and management
/// </summary>
public interface IAspNetUserService
{
    /// <summary>
    /// Retrieves a user by their unique identifier
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<IdentityUser?> GetUserByIdAsync(Guid userId);
    
    /// <summary>
    /// Retrieves a user by their email address
    /// </summary>
    /// <param name="email">The email address of the user</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<IdentityUser?> GetUserByEmailAsync(string email);
    
    /// <summary>
    /// Retrieves a user by their username
    /// </summary>
    /// <param name="username">The username of the user</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<IdentityUser?> GetUserByUsernameAsync(string username);

    /// <summary>
    /// Creates a new user account using ASP.NET Core Identity
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="password">The user's password (will be hashed by Identity)</param>
    /// <param name="firstName">The user's first name (optional)</param>
    /// <param name="lastName">The user's last name (optional)</param>
    /// <param name="userName">The user's username (optional, defaults to email)</param>
    /// <returns>Identity result indicating success or failure</returns>
    Task<Microsoft.AspNetCore.Identity.IdentityResult> CreateUserAsync(string email, string password, string? firstName = null, string? lastName = null, string? userName = null);
    
    /// <summary>
    /// Updates an existing user's information
    /// </summary>
    /// <param name="user">The user to update</param>
    /// <returns>Identity result indicating success or failure</returns>
    Task<Microsoft.AspNetCore.Identity.IdentityResult> UpdateUserAsync(IdentityUser user);
    
    /// <summary>
    /// Deletes a user account
    /// </summary>
    /// <param name="userId">The ID of the user to delete</param>
    /// <returns>Identity result indicating success or failure</returns>
    Task<Microsoft.AspNetCore.Identity.IdentityResult> DeleteUserAsync(Guid userId);

    /// <summary>
    /// Signs in a user using email and password
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="password">The user's password</param>
    /// <param name="rememberMe">Whether to create a persistent cookie</param>
    /// <param name="lockoutOnFailure">Whether to lockout user on failure</param>
    /// <returns>Sign-in result indicating success, failure, or additional requirements</returns>
    Task<Microsoft.AspNetCore.Identity.SignInResult> SignInAsync(string email, string password, bool rememberMe = false, bool lockoutOnFailure = true);
    
    /// <summary>
    /// Signs out the current user
    /// </summary>
    Task SignOutAsync();

    /// <summary>
    /// Changes a user's password after validating their current password
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="currentPassword">The user's current password</param>
    /// <param name="newPassword">The new password to set</param>
    /// <returns>Identity result indicating success or failure</returns>
    Task<Microsoft.AspNetCore.Identity.IdentityResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    
    /// <summary>
    /// Generates a password reset token for a user
    /// </summary>
    /// <param name="user">The user to generate a token for</param>
    /// <returns>The password reset token</returns>
    Task<string> GeneratePasswordResetTokenAsync(IdentityUser user);
    
    /// <summary>
    /// Resets a user's password using a reset token
    /// </summary>
    /// <param name="user">The user whose password to reset</param>
    /// <param name="token">The password reset token</param>
    /// <param name="newPassword">The new password to set</param>
    /// <returns>Identity result indicating success or failure</returns>
    Task<Microsoft.AspNetCore.Identity.IdentityResult> ResetPasswordAsync(IdentityUser user, string token, string newPassword);

    /// <summary>
    /// Generates an email confirmation token for a user
    /// </summary>
    /// <param name="user">The user to generate a token for</param>
    /// <returns>The email confirmation token</returns>
    Task<string> GenerateEmailConfirmationTokenAsync(IdentityUser user);
    
    /// <summary>
    /// Confirms a user's email address using a confirmation token
    /// </summary>
    /// <param name="user">The user whose email to confirm</param>
    /// <param name="token">The email confirmation token</param>
    /// <returns>Identity result indicating success or failure</returns>
    Task<Microsoft.AspNetCore.Identity.IdentityResult> ConfirmEmailAsync(IdentityUser user, string token);

    /// <summary>
    /// Adds a user to a role
    /// </summary>
    /// <param name="user">The user to add to the role</param>
    /// <param name="role">The role name</param>
    /// <returns>Identity result indicating success or failure</returns>
    Task<Microsoft.AspNetCore.Identity.IdentityResult> AddToRoleAsync(IdentityUser user, string role);
    
    /// <summary>
    /// Removes a user from a role
    /// </summary>
    /// <param name="user">The user to remove from the role</param>
    /// <param name="role">The role name</param>
    /// <returns>Identity result indicating success or failure</returns>
    Task<Microsoft.AspNetCore.Identity.IdentityResult> RemoveFromRoleAsync(IdentityUser user, string role);
    
    /// <summary>
    /// Gets all roles assigned to a user
    /// </summary>
    /// <param name="user">The user to get roles for</param>
    /// <returns>List of role names</returns>
    Task<IList<string>> GetRolesAsync(IdentityUser user);
    
    /// <summary>
    /// Checks if a user is in a specific role
    /// </summary>
    /// <param name="user">The user to check</param>
    /// <param name="role">The role name</param>
    /// <returns>True if user is in role, false otherwise</returns>
    Task<bool> IsInRoleAsync(IdentityUser user, string role);

    /// <summary>
    /// Enables or disables two-factor authentication for a user
    /// </summary>
    /// <param name="user">The user to configure</param>
    /// <param name="enabled">Whether to enable or disable 2FA</param>
    /// <returns>Identity result indicating success or failure</returns>
    Task<Microsoft.AspNetCore.Identity.IdentityResult> SetTwoFactorEnabledAsync(IdentityUser user, bool enabled);
    
    /// <summary>
    /// Generates a two-factor authentication token for a user
    /// </summary>
    /// <param name="user">The user to generate a token for</param>
    /// <param name="tokenProvider">The token provider name</param>
    /// <returns>The 2FA token</returns>
    Task<string> GenerateTwoFactorTokenAsync(IdentityUser user, string tokenProvider);
    
    /// <summary>
    /// Verifies a two-factor authentication token
    /// </summary>
    /// <param name="user">The user to verify the token for</param>
    /// <param name="tokenProvider">The token provider name</param>
    /// <param name="token">The token to verify</param>
    /// <returns>True if token is valid, false otherwise</returns>
    Task<bool> VerifyTwoFactorTokenAsync(IdentityUser user, string tokenProvider, string token);

    /// <summary>
    /// Creates a new user session
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="sessionToken">The session token</param>
    /// <param name="ipAddress">The client's IP address</param>
    /// <param name="userAgent">The client's user agent</param>
    /// <param name="sessionType">The type of session</param>
    /// <returns>The created session</returns>
    Task<IdentityUserSession> CreateSessionAsync(Guid userId, string sessionToken, string? ipAddress = null, string? userAgent = null, SessionType sessionType = SessionType.Web);
    
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
    Task RevokeAllIdentityUserSessionsAsync(Guid userId);

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
    Task<List<IdentityLoginHistory>> GetLoginHistoryAsync(Guid userId, int take = 50);

    // Group Management
    Task<bool> AddToGroupAsync(Guid userId, Guid groupId);
    Task<bool> RemoveFromGroupAsync(Guid userId, Guid groupId);
}