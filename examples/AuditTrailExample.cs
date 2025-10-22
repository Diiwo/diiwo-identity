using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Diiwo.Core.Domain.Enums;
using Diiwo.Identity.App.DbContext;
using Diiwo.Identity.App.Entities;
using Diiwo.Identity.App.Application.Services;

namespace Diiwo.Identity.Examples;

/// <summary>
/// Demonstrates the automatic audit trail capabilities of the DIIWO Identity Solution
/// Shows how CreatedAt, UpdatedAt, CreatedBy, UpdatedBy are automatically tracked
/// </summary>
public class AuditTrailExample
{
    private readonly AppIdentityDbContext _context;
    private readonly AppUserService _userService;
    private readonly AppPermissionService _permissionService;
    private readonly ILogger<AuditTrailExample> _logger;

    public AuditTrailExample(
        AppIdentityDbContext context,
        AppUserService userService,
        AppPermissionService permissionService,
        ILogger<AuditTrailExample> logger)
    {
        _context = context;
        _userService = userService;
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <summary>
    /// Complete example showing automatic audit trail for user lifecycle
    /// </summary>
    public async Task DemonstrateUserLifecycleAuditTrailAsync()
    {
        _logger.LogInformation("=== User Lifecycle Audit Trail Example ===");

        // 1. Create a user - audit fields set automatically by AuditInterceptor
        var user = await _userService.CreateUserAsync(
            "audit-demo@example.com",
            BCrypt.Net.BCrypt.HashPassword("SecurePassword123!"),
            "Audit",
            "Demo"
        );

        _logger.LogInformation("User created - ID: {UserId}", user.Id);
        _logger.LogInformation("  CreatedAt: {CreatedAt}", user.CreatedAt);
        _logger.LogInformation("  UpdatedAt: {UpdatedAt}", user.UpdatedAt);
        _logger.LogInformation("  CreatedBy: {CreatedBy}", user.CreatedBy);
        _logger.LogInformation("  UpdatedBy: {UpdatedBy}", user.UpdatedBy);
        _logger.LogInformation("  State: {State}", user.State);

        // Wait a moment to see timestamp difference
        await Task.Delay(1000);

        // 2. Update the user - UpdatedAt and UpdatedBy set automatically
        user.FirstName = "Updated Audit";
        user.LastName = "Updated Demo";
        await _userService.UpdateUserAsync(user);

        // Reload to see updated audit fields
        var updatedUser = await _userService.GetUserByIdAsync(user.Id);
        _logger.LogInformation("\nUser updated - ID: {UserId}", updatedUser!.Id);
        _logger.LogInformation("  CreatedAt: {CreatedAt} (unchanged)", updatedUser.CreatedAt);
        _logger.LogInformation("  UpdatedAt: {UpdatedAt} (automatically updated)", updatedUser.UpdatedAt);
        _logger.LogInformation("  CreatedBy: {CreatedBy} (unchanged)", updatedUser.CreatedBy);
        _logger.LogInformation("  UpdatedBy: {UpdatedBy} (automatically updated)", updatedUser.UpdatedBy);

        // 3. Demonstrate soft delete - State changes to Terminated
        await _userService.DeleteUserAsync(user.Id);

        // Query including soft-deleted entities
        var deletedUser = await _context.Users
            .IgnoreQueryFilters() // Bypass soft delete filter
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        _logger.LogInformation("\nUser soft deleted - ID: {UserId}", deletedUser!.Id);
        _logger.LogInformation("  State: {State} (changed to Terminated)", deletedUser.State);
        _logger.LogInformation("  UpdatedAt: {UpdatedAt} (automatically updated on delete)", deletedUser.UpdatedAt);
        _logger.LogInformation("  UpdatedBy: {UpdatedBy} (automatically updated on delete)", deletedUser.UpdatedBy);

        _logger.LogInformation("=== End User Lifecycle Audit Trail Example ===\n");
    }

    /// <summary>
    /// Demonstrates permission assignment audit trail
    /// </summary>
    public async Task DemonstratePermissionAuditTrailAsync()
    {
        _logger.LogInformation("=== Permission Assignment Audit Trail Example ===");

        // Create a test user
        var user = await _userService.CreateUserAsync(
            "permission-demo@example.com",
            BCrypt.Net.BCrypt.HashPassword("SecurePassword123!"),
            "Permission",
            "Demo"
        );

        // Create a permission
        var permission = await _permissionService.CreatePermissionAsync(
            "Document",
            "Read",
            "Permission to read documents"
        );

        _logger.LogInformation("Permission created - ID: {PermissionId}", permission.Id);
        _logger.LogInformation("  Name: {PermissionName}", permission.Name);
        _logger.LogInformation("  CreatedAt: {CreatedAt}", permission.CreatedAt);
        _logger.LogInformation("  CreatedBy: {CreatedBy}", permission.CreatedBy);

        // Grant permission to user - creates AppUserPermission with automatic audit
        await _permissionService.GrantUserPermissionAsync(
            user.Id,
            permission.Id,
            isGranted: true,
            priority: 100,
            expiresAt: DateTime.UtcNow.AddDays(30)
        );

        // Check the audit trail of the permission assignment
        var userPermission = await _context.UserPermissions
            .Include(up => up.Permission)
            .Include(up => up.User)
            .FirstOrDefaultAsync(up => up.UserId == user.Id && up.PermissionId == permission.Id);

        _logger.LogInformation("\nUser permission granted:");
        _logger.LogInformation("  User: {UserEmail}", userPermission!.User.Email);
        _logger.LogInformation("  Permission: {PermissionName}", userPermission.Permission.Name);
        _logger.LogInformation("  IsGranted: {IsGranted}", userPermission.IsGranted);
        _logger.LogInformation("  ExpiresAt: {ExpiresAt}", userPermission.ExpiresAt);
        _logger.LogInformation("  CreatedAt: {CreatedAt}", userPermission.CreatedAt);
        _logger.LogInformation("  CreatedBy: {CreatedBy}", userPermission.CreatedBy);

        // Wait and update the permission
        await Task.Delay(1000);
        await _permissionService.GrantUserPermissionAsync(
            user.Id,
            permission.Id,
            isGranted: false, // Revoke permission
            priority: 100
        );

        // Check updated audit trail
        var updatedUserPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == user.Id && up.PermissionId == permission.Id);

        _logger.LogInformation("\nUser permission updated (revoked):");
        _logger.LogInformation("  IsGranted: {IsGranted}", updatedUserPermission!.IsGranted);
        _logger.LogInformation("  CreatedAt: {CreatedAt} (unchanged)", updatedUserPermission.CreatedAt);
        _logger.LogInformation("  UpdatedAt: {UpdatedAt} (automatically updated)", updatedUserPermission.UpdatedAt);
        _logger.LogInformation("  UpdatedBy: {UpdatedBy} (automatically updated)", updatedUserPermission.UpdatedBy);

        _logger.LogInformation("=== End Permission Assignment Audit Trail Example ===\n");
    }

    /// <summary>
    /// Demonstrates group management audit trail
    /// </summary>
    public async Task DemonstrateGroupAuditTrailAsync()
    {
        _logger.LogInformation("=== Group Management Audit Trail Example ===");

        // Create a group - audit fields set automatically
        var group = new AppGroup
        {
            Name = "Audit Demo Group",
            Description = "Group for demonstrating audit trail functionality"
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Group created - ID: {GroupId}", group.Id);
        _logger.LogInformation("  Name: {GroupName}", group.Name);
        _logger.LogInformation("  CreatedAt: {CreatedAt}", group.CreatedAt);
        _logger.LogInformation("  State: {State}", group.State);
        _logger.LogInformation("  IsActive: {IsActive}", group.IsActive);

        // Create a permission for the group
        var permission = await _permissionService.CreatePermissionAsync(
            "Report",
            "Generate",
            "Permission to generate reports"
        );

        // Grant permission to group
        await _permissionService.GrantGroupPermissionAsync(
            group.Id,
            permission.Id,
            isGranted: true,
            priority: 50
        );

        var groupPermission = await _context.GroupPermissions
            .Include(gp => gp.Group)
            .Include(gp => gp.Permission)
            .FirstOrDefaultAsync(gp => gp.GroupId == group.Id && gp.PermissionId == permission.Id);

        _logger.LogInformation("\nGroup permission granted:");
        _logger.LogInformation("  Group: {GroupName}", groupPermission!.Group.Name);
        _logger.LogInformation("  Permission: {PermissionName}", groupPermission.Permission.Name);
        _logger.LogInformation("  Priority: {Priority}", groupPermission.Priority);
        _logger.LogInformation("  CreatedAt: {CreatedAt}", groupPermission.CreatedAt);
        _logger.LogInformation("  CreatedBy: {CreatedBy}", groupPermission.CreatedBy);

        _logger.LogInformation("=== End Group Management Audit Trail Example ===\n");
    }

    /// <summary>
    /// Demonstrates session management audit trail
    /// </summary>
    public async Task DemonstrateSessionAuditTrailAsync()
    {
        _logger.LogInformation("=== Session Management Audit Trail Example ===");

        // Create a user for session demo
        var user = await _userService.CreateUserAsync(
            "session-demo@example.com",
            BCrypt.Net.BCrypt.HashPassword("SecurePassword123!"),
            "Session",
            "Demo"
        );

        // Create a session - audit fields set automatically
        var session = await _userService.CreateSessionAsync(
            user.Id,
            Guid.NewGuid().ToString("N"),
            "192.168.1.100",
            "Mozilla/5.0 (Demo Browser)",
            Shared.Enums.SessionType.Web
        );

        _logger.LogInformation("Session created - ID: {SessionId}", session.Id);
        _logger.LogInformation("  UserId: {UserId}", session.UserId);
        _logger.LogInformation("  SessionToken: {SessionToken}", session.SessionToken[..8] + "...");
        _logger.LogInformation("  IpAddress: {IpAddress}", session.IpAddress);
        _logger.LogInformation("  ExpiresAt: {ExpiresAt}", session.ExpiresAt);
        _logger.LogInformation("  CreatedAt: {CreatedAt}", session.CreatedAt);
        _logger.LogInformation("  IsActive: {IsActive}", session.IsActive);

        // Wait and revoke session
        await Task.Delay(1000);
        await _userService.RevokeSessionAsync(session.SessionToken);

        // Check updated session
        var revokedSession = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.Id == session.Id);

        _logger.LogInformation("\nSession revoked:");
        _logger.LogInformation("  IsActive: {IsActive}", revokedSession!.IsActive);
        _logger.LogInformation("  CreatedAt: {CreatedAt} (unchanged)", revokedSession.CreatedAt);
        _logger.LogInformation("  UpdatedAt: {UpdatedAt} (automatically updated)", revokedSession.UpdatedAt);
        _logger.LogInformation("  UpdatedBy: {UpdatedBy} (automatically updated)", revokedSession.UpdatedBy);

        _logger.LogInformation("=== End Session Management Audit Trail Example ===\n");
    }

    /// <summary>
    /// Demonstrates login history audit trail
    /// </summary>
    public async Task DemonstrateLoginHistoryAuditTrailAsync()
    {
        _logger.LogInformation("=== Login History Audit Trail Example ===");

        // Create a user for login demo
        var user = await _userService.CreateUserAsync(
            "login-demo@example.com",
            BCrypt.Net.BCrypt.HashPassword("SecurePassword123!"),
            "Login",
            "Demo"
        );

        // Log some login attempts - audit fields set automatically
        await _userService.LogLoginAttemptAsync(
            user.Id,
            isSuccessful: true,
            ipAddress: "192.168.1.100",
            userAgent: "Mozilla/5.0 (Demo Browser)",
            authMethod: Shared.Enums.AuthMethod.EmailPassword
        );

        await Task.Delay(500);

        await _userService.LogLoginAttemptAsync(
            user.Id,
            isSuccessful: false,
            ipAddress: "192.168.1.200",
            userAgent: "Mozilla/5.0 (Suspicious Browser)",
            failureReason: "Invalid password",
            authMethod: Shared.Enums.AuthMethod.EmailPassword
        );

        // Get login history
        var loginHistory = await _userService.GetLoginHistoryAsync(user.Id, 10);

        _logger.LogInformation("Login history for user {UserEmail}:", user.Email);
        foreach (var entry in loginHistory)
        {
            _logger.LogInformation("  Attempt at {LoginAttemptAt}:", entry.LoginAttemptAt);
            _logger.LogInformation("    Success: {IsSuccessful}", entry.IsSuccessful);
            _logger.LogInformation("    IP: {IpAddress}", entry.IpAddress);
            _logger.LogInformation("    Method: {AuthMethod}", entry.AuthMethod);
            _logger.LogInformation("    CreatedAt: {CreatedAt}", entry.CreatedAt);
            _logger.LogInformation("    CreatedBy: {CreatedBy}", entry.CreatedBy);
            if (!string.IsNullOrEmpty(entry.FailureReason))
            {
                _logger.LogInformation("    Failure Reason: {FailureReason}", entry.FailureReason);
            }
            _logger.LogInformation("");
        }

        _logger.LogInformation("=== End Login History Audit Trail Example ===\n");
    }

    /// <summary>
    /// Runs all audit trail examples
    /// </summary>
    public async Task RunAllExamplesAsync()
    {
        _logger.LogInformation("🔍 DIIWO Identity Solution - Audit Trail Capabilities Demo");
        _logger.LogInformation("================================================================");

        await DemonstrateUserLifecycleAuditTrailAsync();
        await DemonstratePermissionAuditTrailAsync();
        await DemonstrateGroupAuditTrailAsync();
        await DemonstrateSessionAuditTrailAsync();
        await DemonstrateLoginHistoryAuditTrailAsync();

        _logger.LogInformation("✅ All audit trail examples completed successfully!");
        _logger.LogInformation("================================================================");
    }
}

/// <summary>
/// Console application entry point for running audit trail examples
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add Entity Framework with in-memory database for demo
        services.AddDbContext<AppIdentityDbContext>(options =>
            options.UseInMemoryDatabase("AuditTrailDemo"));

        // Add identity services
        services.AddScoped<AppUserService>();
        services.AddScoped<AppPermissionService>();
        services.AddScoped<AuditTrailExample>();

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Run examples
        using (var scope = serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
            await context.Database.EnsureCreatedAsync();

            var example = scope.ServiceProvider.GetRequiredService<AuditTrailExample>();
            await example.RunAllExamplesAsync();
        }
    }
}