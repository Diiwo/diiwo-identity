using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Diiwo.Identity.App.DbContext;
using Diiwo.Identity.App.Entities;
using Diiwo.Identity.App.Abstractions.Services;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.App.Application.Services;

/// <summary>
/// APP ARCHITECTURE - Simple user management service
/// Standalone implementation without ASP.NET Core Identity complexity
/// Basic CRUD operations with authentication features
/// </summary>
public class AppUserService : IAppUserService
{
    private readonly AppIdentityDbContext _context;
    private readonly ILogger<AppUserService> _logger;

    public AppUserService(AppIdentityDbContext context, ILogger<AppUserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AppUser?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users
            .Include(u => u.UserSessions)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .Include(u => u.UserGroups)
                .ThenInclude(g => g.GroupPermissions)
                    .ThenInclude(gp => gp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    /// <inheritdoc />
    public async Task<AppUser?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.UserSessions)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .Include(u => u.UserGroups)
                .ThenInclude(g => g.GroupPermissions)
                    .ThenInclude(gp => gp.Permission)
            .FirstOrDefaultAsync(u => u.Email == email.ToLower());
    }

    /// <inheritdoc />
    public async Task<AppUser?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .Include(u => u.UserSessions)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .Include(u => u.UserGroups)
                .ThenInclude(g => g.GroupPermissions)
                    .ThenInclude(gp => gp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    /// <inheritdoc />
    public async Task<AppUser> CreateUserAsync(string email, string passwordHash, string? firstName = null, string? lastName = null)
    {
        // Note: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy are set automatically by AuditInterceptor
        var user = new AppUser
        {
            Email = email.ToLower(),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User created: {UserId} - {Email}", user.Id, user.Email);
        return user;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateUserAsync(AppUser user)
    {
        try
        {
            // Note: UpdatedAt and UpdatedBy are set automatically by AuditInterceptor
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("User updated: {UserId} - {Email}", user.Id, user.Email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user: {UserId}", user.Id);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("User deleted: {UserId} - {Email}", user.Id, user.Email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user: {UserId}", userId);
            return false;
        }
    }

    // Authentication
    /// <inheritdoc />
    public async Task<bool> ValidatePasswordAsync(AppUser user, string password)
    {
        // In a real implementation, you would use proper password hashing
        // This is a simplified example
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    /// <inheritdoc />
    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        if (!await ValidatePasswordAsync(user, currentPassword))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        // Note: UpdatedAt and UpdatedBy are set automatically by AuditInterceptor

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Password changed for user: {UserId}", userId);
        return true;
    }

    // Email Confirmation
    /// <inheritdoc />
    public async Task<bool> ConfirmEmailAsync(Guid userId, string token)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.EmailConfirmationToken != token) return false;

        user.EmailConfirmed = true;
        user.EmailConfirmationToken = null;
        // Note: UpdatedAt and UpdatedBy are set automatically by AuditInterceptor

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Email confirmed for user: {UserId}", userId);
        return true;
    }

    /// <inheritdoc />
    public async Task<string> GenerateEmailConfirmationTokenAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) throw new ArgumentException("User not found", nameof(userId));

        var token = Guid.NewGuid().ToString();
        user.EmailConfirmationToken = token;
        // Note: UpdatedAt and UpdatedBy are set automatically by AuditInterceptor

        await _context.SaveChangesAsync();
        return token;
    }

    // Password Reset
    /// <inheritdoc />
    public async Task<string?> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await GetUserByEmailAsync(email);
        if (user == null) return null;

        var token = Guid.NewGuid().ToString();
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1);
        // Note: UpdatedAt and UpdatedBy are set automatically by AuditInterceptor

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Password reset token generated for user: {UserId}", user.Id);
        return token;
    }

    /// <inheritdoc />
    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token && 
                                    u.PasswordResetTokenExpires > DateTime.UtcNow);
        
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpires = null;
        // Note: UpdatedAt and UpdatedBy are set automatically by AuditInterceptor

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Password reset for user: {UserId}", user.Id);
        return true;
    }

    // Session Management
    /// <inheritdoc />
    public async Task<AppUserSession> CreateSessionAsync(Guid userId, string sessionToken, string? ipAddress = null, string? userAgent = null, SessionType sessionType = SessionType.Web)
    {
        // Note: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy are set automatically by AuditInterceptor
        var session = new AppUserSession
        {
            UserId = userId,
            SessionToken = sessionToken,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SessionType = sessionType,
            ExpiresAt = DateTime.UtcNow.AddDays(30) // 30 days default
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        return session;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateSessionAsync(string sessionToken)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken &&
                                    s.State == Diiwo.Core.Domain.Enums.EntityState.Active &&
                                    s.ExpiresAt > DateTime.UtcNow);

        return session != null;
    }

    /// <inheritdoc />
    public async Task<bool> RevokeSessionAsync(string sessionToken)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);
        
        if (session == null) return false;

        session.State = Diiwo.Core.Domain.Enums.EntityState.Inactive;
        // Note: UpdatedAt and UpdatedBy are set automatically by AuditInterceptor

        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task RevokeAllUserSessionsAsync(Guid userId)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.State == Diiwo.Core.Domain.Enums.EntityState.Active)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.State = Diiwo.Core.Domain.Enums.EntityState.Inactive;
            // Note: UpdatedAt and UpdatedBy are set automatically by AuditInterceptor
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("All sessions revoked for user: {UserId}", userId);
    }

    // Login History
    /// <inheritdoc />
    public async Task LogLoginAttemptAsync(Guid userId, bool isSuccessful, string? ipAddress = null, string? userAgent = null, string? failureReason = null, AuthMethod authMethod = AuthMethod.Password)
    {
        // Note: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy are set automatically by AuditInterceptor
        var loginHistory = new AppUserLoginHistory
        {
            UserId = userId,
            IsSuccessful = isSuccessful,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            FailureReason = failureReason,
            AuthMethod = authMethod,
            LoginAttemptAt = DateTime.UtcNow
        };

        _context.LoginHistory.Add(loginHistory);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<AppUserLoginHistory>> GetLoginHistoryAsync(Guid userId, int take = 50)
    {
        return await _context.LoginHistory
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.LoginAttemptAt)
            .Take(take)
            .ToListAsync();
    }
}