using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.AspNet.Abstractions.Services;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.AspNet.Application.Services;

/// <summary>
///  ASPNET ARCHITECTURE - Enterprise user management service with ASP.NET Core Identity
/// Full integration with UserManager, RoleManager, and enterprise features
/// Built on top of ASP.NET Core Identity for maximum compatibility
/// </summary>
public class AspNetUserService : IAspNetUserService
{
    private readonly Microsoft.AspNetCore.Identity.UserManager<IdentityUser> _userManager;
    private readonly Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> _roleManager;
    private readonly Microsoft.AspNetCore.Identity.SignInManager<IdentityUser> _signInManager;
    private readonly AspNetIdentityDbContext _context;
    private readonly ILogger<AspNetUserService> _logger;

    public AspNetUserService(
        Microsoft.AspNetCore.Identity.UserManager<IdentityUser> userManager,
        Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> roleManager,
        Microsoft.AspNetCore.Identity.SignInManager<IdentityUser> signInManager,
        AspNetIdentityDbContext context,
        ILogger<AspNetUserService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _context = context;
        _logger = logger;
    }

    // User Management with ASP.NET Core Identity
    /// <inheritdoc />
    public async Task<IdentityUser?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users
            .Include(u => u.IdentityUserSessions)
            .Include(u => u.IdentityUserPermissions)
                .ThenInclude(up => up.Permission)
            .Include(u => u.IdentityUserGroups)
                .ThenInclude(g => g.GroupPermissions)
                    .ThenInclude(gp => gp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    /// <inheritdoc />
    public async Task<IdentityUser?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    /// <inheritdoc />
    public async Task<IdentityUser?> GetUserByUsernameAsync(string username)
    {
        return await _userManager.FindByNameAsync(username);
    }

    /// <inheritdoc />
    public async Task<Microsoft.AspNetCore.Identity.IdentityResult> CreateUserAsync(string email, string password, string? firstName = null, string? lastName = null, string? userName = null)
    {
        var user = new IdentityUser
        {
            Email = email,
            UserName = userName ?? email,
            EmailConfirmed = false,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("User created: {UserId} - {Email}", user.Id, user.Email);
        }
        else
        {
            _logger.LogWarning("Failed to create user {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Microsoft.AspNetCore.Identity.IdentityResult> UpdateUserAsync(IdentityUser user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("User updated: {UserId} - {Email}", user.Id, user.Email);
        }
        else
        {
            _logger.LogWarning("Failed to update user {UserId}: {Errors}", user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Microsoft.AspNetCore.Identity.IdentityResult> DeleteUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) 
        {
            return Microsoft.AspNetCore.Identity.IdentityResult.Failed(new Microsoft.AspNetCore.Identity.IdentityError { Description = "User not found" });
        }

        var result = await _userManager.DeleteAsync(user);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("User deleted: {UserId} - {Email}", user.Id, user.Email);
        }

        return result;
    }

    // Authentication with ASP.NET Core Identity
    /// <inheritdoc />
    public async Task<Microsoft.AspNetCore.Identity.SignInResult> SignInAsync(string email, string password, bool rememberMe = false, bool lockoutOnFailure = true)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            await LogLoginAttemptAsync(Guid.Empty, false, failureReason: "User not found", authMethod: AuthMethod.Password);
            return Microsoft.AspNetCore.Identity.SignInResult.Failed;
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, password, rememberMe, lockoutOnFailure);
        
        await LogLoginAttemptAsync(user.Id, result.Succeeded, 
            failureReason: result.Succeeded ? null : GetSignInFailureReason(result),
            authMethod: AuthMethod.Password);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task SignOutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    // Password Management
    /// <inheritdoc />
    public async Task<Microsoft.AspNetCore.Identity.IdentityResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Microsoft.AspNetCore.Identity.IdentityResult.Failed(new Microsoft.AspNetCore.Identity.IdentityError { Description = "User not found" });
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Password changed for user: {UserId}", userId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<string> GeneratePasswordResetTokenAsync(IdentityUser user)
    {
        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    /// <inheritdoc />
    public async Task<Microsoft.AspNetCore.Identity.IdentityResult> ResetPasswordAsync(IdentityUser user, string token, string newPassword)
    {
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Password reset for user: {UserId}", user.Id);
        }

        return result;
    }

    // Email Confirmation
    /// <inheritdoc />
    public async Task<string> GenerateEmailConfirmationTokenAsync(IdentityUser user)
    {
        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    /// <inheritdoc />
    public async Task<Microsoft.AspNetCore.Identity.IdentityResult> ConfirmEmailAsync(IdentityUser user, string token)
    {
        var result = await _userManager.ConfirmEmailAsync(user, token);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Email confirmed for user: {UserId}", user.Id);
        }

        return result;
    }

    // Role Management
    /// <inheritdoc />
    public async Task<Microsoft.AspNetCore.Identity.IdentityResult> AddToRoleAsync(IdentityUser user, string role)
    {
        var result = await _userManager.AddToRoleAsync(user, role);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} added to role: {Role}", user.Id, role);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Microsoft.AspNetCore.Identity.IdentityResult> RemoveFromRoleAsync(IdentityUser user, string role)
    {
        var result = await _userManager.RemoveFromRoleAsync(user, role);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} removed from role: {Role}", user.Id, role);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IList<string>> GetRolesAsync(IdentityUser user)
    {
        return await _userManager.GetRolesAsync(user);
    }

    /// <inheritdoc />
    public async Task<bool> IsInRoleAsync(IdentityUser user, string role)
    {
        return await _userManager.IsInRoleAsync(user, role);
    }

    // Two-Factor Authentication
    /// <inheritdoc />
    public async Task<Microsoft.AspNetCore.Identity.IdentityResult> SetTwoFactorEnabledAsync(IdentityUser user, bool enabled)
    {
        var result = await _userManager.SetTwoFactorEnabledAsync(user, enabled);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Two-factor authentication {Status} for user: {UserId}", 
                enabled ? "enabled" : "disabled", user.Id);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<string> GenerateTwoFactorTokenAsync(IdentityUser user, string tokenProvider)
    {
        return await _userManager.GenerateTwoFactorTokenAsync(user, tokenProvider);
    }

    /// <inheritdoc />
    public async Task<bool> VerifyTwoFactorTokenAsync(IdentityUser user, string tokenProvider, string token)
    {
        return await _userManager.VerifyTwoFactorTokenAsync(user, tokenProvider, token);
    }

    // Session Management (Custom Implementation)
    /// <inheritdoc />
    public async Task<IdentityUserSession> CreateSessionAsync(Guid userId, string sessionToken, string? ipAddress = null, string? userAgent = null, SessionType sessionType = SessionType.Web)
    {
        var session = new IdentityUserSession
        {
            UserId = userId,
            SessionToken = sessionToken,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SessionType = sessionType,
            ExpiresAt = DateTime.UtcNow.AddDays(30), // 30 days default
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.IdentityUserSessions.Add(session);
        await _context.SaveChangesAsync();

        return session;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateSessionAsync(string sessionToken)
    {
        var session = await _context.IdentityUserSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && 
                                    s.IsActive && 
                                    s.ExpiresAt > DateTime.UtcNow);
        
        return session != null;
    }

    /// <inheritdoc />
    public async Task<bool> RevokeSessionAsync(string sessionToken)
    {
        var session = await _context.IdentityUserSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);
        
        if (session == null) return false;

        session.IsActive = false;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task RevokeAllIdentityUserSessionsAsync(Guid userId)
    {
        var sessions = await _context.IdentityUserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("All sessions revoked for user: {UserId}", userId);
    }

    // Login History
    /// <inheritdoc />
    public async Task LogLoginAttemptAsync(Guid userId, bool isSuccessful, string? ipAddress = null, string? userAgent = null, string? failureReason = null, AuthMethod authMethod = AuthMethod.Password)
    {
        var loginHistory = new IdentityLoginHistory
        {
            UserId = userId,
            IsSuccessful = isSuccessful,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            FailureReason = failureReason,
            AuthMethod = authMethod,
            LoginAttemptAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.IdentityLoginHistory.Add(loginHistory);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<IdentityLoginHistory>> GetLoginHistoryAsync(Guid userId, int take = 50)
    {
        return await _context.IdentityLoginHistory
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.LoginAttemptAt)
            .Take(take)
            .ToListAsync();
    }

    // Group Management
    /// <inheritdoc />
    public async Task<bool> AddToGroupAsync(Guid userId, Guid groupId)
    {
        var user = await GetUserByIdAsync(userId);
        var group = await _context.IdentityGroups.FindAsync(groupId);
        
        if (user == null || group == null) return false;

        if (!user.IdentityUserGroups.Any(g => g.Id == groupId))
        {
            user.IdentityUserGroups.Add(group);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("User {UserId} added to group: {GroupId}", userId, groupId);
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveFromGroupAsync(Guid userId, Guid groupId)
    {
        var user = await GetUserByIdAsync(userId);
        var group = user?.IdentityUserGroups.FirstOrDefault(g => g.Id == groupId);
        
        if (user == null || group == null) return false;

        user.IdentityUserGroups.Remove(group);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("User {UserId} removed from group: {GroupId}", userId, groupId);
        return true;
    }

    // Helper Methods
    private static string GetSignInFailureReason(Microsoft.AspNetCore.Identity.SignInResult result)
    {
        if (result.IsLockedOut) return "Account locked";
        if (result.IsNotAllowed) return "Sign in not allowed";
        if (result.RequiresTwoFactor) return "Two-factor required";
        return "Invalid credentials";
    }
}