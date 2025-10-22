using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Diiwo.Core.Domain.Enums;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.AspNet.Application.Services;

namespace Diiwo.Identity.Examples;

/// <summary>
/// Demonstrates the automatic audit trail capabilities of the DIIWO Identity Solution
/// for the AspNet architecture (enterprise features with ASP.NET Core Identity)
/// </summary>
public class AspNetAuditTrailExample
{
    private readonly AspNetIdentityDbContext _context;
    private readonly AspNetUserService _userService;
    private readonly AspNetPermissionService _permissionService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AspNetAuditTrailExample> _logger;

    public AspNetAuditTrailExample(
        AspNetIdentityDbContext context,
        AspNetUserService userService,
        AspNetPermissionService permissionService,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AspNetAuditTrailExample> logger)
    {
        _context = context;
        _userService = userService;
        _permissionService = permissionService;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Demonstrates AspNet Identity user with automatic audit trail
    /// </summary>
    public async Task DemonstrateAspNetUserAuditTrailAsync()
    {
        _logger.LogInformation("=== AspNet Identity User Audit Trail Example ===");

        // Create user through AspNet Identity with automatic audit
        var createResult = await _userService.CreateUserAsync(
            "aspnet-audit@example.com",
            "SecurePassword123!",
            "AspNet",
            "Audit Demo",
            "aspnet-audit"
        );

        if (createResult.Succeeded)
        {
            var user = await _userService.GetUserByEmailAsync("aspnet-audit@example.com");

            _logger.LogInformation("AspNet Identity User created - ID: {UserId}", user!.Id);
            _logger.LogInformation("  Email: {Email}", user.Email);
            _logger.LogInformation("  UserName: {UserName}", user.UserName);
            _logger.LogInformation("  FirstName: {FirstName}", user.FirstName);
            _logger.LogInformation("  LastName: {LastName}", user.LastName);
            _logger.LogInformation("  CreatedAt: {CreatedAt}", user.CreatedAt);
            _logger.LogInformation("  UpdatedAt: {UpdatedAt}", user.UpdatedAt);
            _logger.LogInformation("  CreatedBy: {CreatedBy}", user.CreatedBy);
            _logger.LogInformation("  State: {State}", user.State);
            _logger.LogInformation("  IsActive: {IsActive}", user.IsActive);

            // Wait and update user - audit fields updated automatically
            await Task.Delay(1000);
            user.FirstName = "Updated AspNet";
            user.LastName = "Updated Demo";

            var updateResult = await _userService.UpdateUserAsync(user);
            if (updateResult.Succeeded)
            {
                // Reload to see updated audit fields
                var updatedUser = await _userService.GetUserByIdAsync(user.Id);
                _logger.LogInformation("\nUser updated:");
                _logger.LogInformation("  CreatedAt: {CreatedAt} (unchanged)", updatedUser!.CreatedAt);
                _logger.LogInformation("  UpdatedAt: {UpdatedAt} (automatically updated)", updatedUser.UpdatedAt);
                _logger.LogInformation("  UpdatedBy: {UpdatedBy} (automatically updated)", updatedUser.UpdatedBy);
            }

            // Demonstrate ASP.NET Identity features with audit trail
            await DemonstrateAspNetIdentityFeaturesAsync(user);
        }

        _logger.LogInformation("=== End AspNet Identity User Audit Trail Example ===\n");
    }

    /// <summary>
    /// Demonstrates ASP.NET Core Identity features with audit trail
    /// </summary>
    private async Task DemonstrateAspNetIdentityFeaturesAsync(IdentityUser user)
    {
        _logger.LogInformation("--- ASP.NET Core Identity Features with Audit Trail ---");

        // 1. Role Management
        var roleResult = await _roleManager.CreateAsync(new IdentityRole("AuditManager"));
        if (roleResult.Succeeded)
        {
            var role = await _roleManager.FindByNameAsync("AuditManager");
            _logger.LogInformation("Role created - ID: {RoleId}, Name: {RoleName}", role!.Id, role.Name);
            _logger.LogInformation("  CreatedAt: {CreatedAt}", role.CreatedAt);
            _logger.LogInformation("  State: {State}", role.State);

            // Add user to role
            await _userService.AddToRoleAsync(user, "AuditManager");
            _logger.LogInformation("User added to role: AuditManager");
        }

        // 2. Email Confirmation with audit trail
        var emailToken = await _userService.GenerateEmailConfirmationTokenAsync(user);
        var confirmResult = await _userService.ConfirmEmailAsync(user, emailToken);
        if (confirmResult.Succeeded)
        {
            _logger.LogInformation("Email confirmed for user");
        }

        // 3. Password Management
        var passwordChangeResult = await _userService.ChangePasswordAsync(
            user.Id,
            "SecurePassword123!",
            "NewSecurePassword456!"
        );
        if (passwordChangeResult.Succeeded)
        {
            _logger.LogInformation("Password changed successfully");
        }

        // 4. Two-Factor Authentication
        var twoFactorResult = await _userService.SetTwoFactorEnabledAsync(user, true);
        if (twoFactorResult.Succeeded)
        {
            _logger.LogInformation("Two-factor authentication enabled");
        }
    }

    /// <summary>
    /// Demonstrates enterprise permission system with audit trail
    /// </summary>
    public async Task DemonstrateEnterprisePermissionAuditTrailAsync()
    {
        _logger.LogInformation("=== Enterprise Permission System Audit Trail Example ===");

        // Create a user for permission demo
        var createResult = await _userService.CreateUserAsync(
            "permission-enterprise@example.com",
            "SecurePassword123!",
            "Permission",
            "Enterprise"
        );

        if (createResult.Succeeded)
        {
            var user = await _userService.GetUserByEmailAsync("permission-enterprise@example.com");

            // Create an enterprise permission
            var permission = new IdentityPermission
            {
                Resource = "EnterpriseDocument",
                Action = "Approve",
                Description = "Permission to approve enterprise documents",
                Scope = Shared.Enums.PermissionScope.Global,
                Priority = 10
            };

            _context.IdentityPermissions.Add(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Enterprise permission created - ID: {PermissionId}", permission.Id);
            _logger.LogInformation("  Name: {PermissionName}", permission.Name);
            _logger.LogInformation("  Scope: {Scope}", permission.Scope);
            _logger.LogInformation("  CreatedAt: {CreatedAt}", permission.CreatedAt);
            _logger.LogInformation("  CreatedBy: {CreatedBy}", permission.CreatedBy);

            // Assign permission to user with audit trail
            await _permissionService.AssignPermissionToUserAsync(user!.Id, permission.Resource, permission.Action, isGranted: true);

            var userPermission = await _context.IdentityUserPermissions
                .Include(up => up.Permission)
                .Include(up => up.User)
                .FirstOrDefaultAsync(up => up.UserId == user.Id && up.PermissionId == permission.Id);

            _logger.LogInformation("\nEnterprise permission assigned:");
            _logger.LogInformation("  User: {UserEmail}", userPermission!.User.Email);
            _logger.LogInformation("  Permission: {PermissionName}", userPermission.Permission.Name);
            _logger.LogInformation("  IsGranted: {IsGranted}", userPermission.IsGranted);
            _logger.LogInformation("  CreatedAt: {CreatedAt}", userPermission.CreatedAt);
            _logger.LogInformation("  CreatedBy: {CreatedBy}", userPermission.CreatedBy);

            // Demonstrate group-based permissions
            await DemonstrateGroupPermissionAuditTrailAsync(user, permission);
        }

        _logger.LogInformation("=== End Enterprise Permission System Audit Trail Example ===\n");
    }

    /// <summary>
    /// Demonstrates group-based permission management with audit trail
    /// </summary>
    private async Task DemonstrateGroupPermissionAuditTrailAsync(IdentityUser user, IdentityPermission permission)
    {
        _logger.LogInformation("--- Enterprise Group Permission Audit Trail ---");

        // Create an enterprise group
        var group = new IdentityGroup
        {
            Name = "Enterprise Approvers",
            Description = "Group for users who can approve enterprise documents"
        };

        _context.IdentityGroups.Add(group);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Enterprise group created - ID: {GroupId}", group.Id);
        _logger.LogInformation("  Name: {GroupName}", group.Name);
        _logger.LogInformation("  CreatedAt: {CreatedAt}", group.CreatedAt);
        _logger.LogInformation("  State: {State}", group.State);

        // Assign permission to group
        await _permissionService.AssignPermissionToGroupAsync(group.Id, permission.Resource, permission.Action, isGranted: true);

        var groupPermission = await _context.IdentityGroupPermissions
            .Include(gp => gp.Group)
            .Include(gp => gp.Permission)
            .FirstOrDefaultAsync(gp => gp.GroupId == group.Id && gp.PermissionId == permission.Id);

        _logger.LogInformation("\nPermission assigned to group:");
        _logger.LogInformation("  Group: {GroupName}", groupPermission!.Group.Name);
        _logger.LogInformation("  Permission: {PermissionName}", groupPermission.Permission.Name);
        _logger.LogInformation("  CreatedAt: {CreatedAt}", groupPermission.CreatedAt);

        // Add user to group
        await _userService.AddToGroupAsync(user.Id, group.Id);
        _logger.LogInformation("User added to enterprise group");
    }

    /// <summary>
    /// Demonstrates session management with audit trail
    /// </summary>
    public async Task DemonstrateSessionManagementAuditTrailAsync()
    {
        _logger.LogInformation("=== Enterprise Session Management Audit Trail Example ===");

        // Create a user for session demo
        var createResult = await _userService.CreateUserAsync(
            "session-enterprise@example.com",
            "SecurePassword123!",
            "Session",
            "Enterprise"
        );

        if (createResult.Succeeded)
        {
            var user = await _userService.GetUserByEmailAsync("session-enterprise@example.com");

            // Create enterprise session with audit trail
            var session = await _userService.CreateSessionAsync(
                user!.Id,
                Guid.NewGuid().ToString("N"),
                "10.0.0.100",
                "Mozilla/5.0 (Enterprise Browser)",
                Shared.Enums.SessionType.Web
            );

            _logger.LogInformation("Enterprise session created - ID: {SessionId}", session.Id);
            _logger.LogInformation("  UserId: {UserId}", session.UserId);
            _logger.LogInformation("  SessionType: {SessionType}", session.SessionType);
            _logger.LogInformation("  IpAddress: {IpAddress}", session.IpAddress);
            _logger.LogInformation("  ExpiresAt: {ExpiresAt}", session.ExpiresAt);
            _logger.LogInformation("  CreatedAt: {CreatedAt}", session.CreatedAt);
            _logger.LogInformation("  IsActive: {IsActive}", session.IsActive);

            // Log enterprise login attempt
            await _userService.LogLoginAttemptAsync(
                user.Id,
                isSuccessful: true,
                ipAddress: "10.0.0.100",
                userAgent: "Mozilla/5.0 (Enterprise Browser)",
                authMethod: Shared.Enums.AuthMethod.EmailPassword
            );

            // Get login history with audit trail
            var loginHistory = await _userService.GetLoginHistoryAsync(user.Id, 5);
            _logger.LogInformation("\nEnterprise login history:");
            foreach (var entry in loginHistory)
            {
                _logger.LogInformation("  Login at {LoginAttemptAt}:", entry.LoginAttemptAt);
                _logger.LogInformation("    Success: {IsSuccessful}", entry.IsSuccessful);
                _logger.LogInformation("    Method: {AuthMethod}", entry.AuthMethod);
                _logger.LogInformation("    CreatedAt: {CreatedAt}", entry.CreatedAt);
                _logger.LogInformation("    State: {State}", entry.State);
            }

            // Revoke session with audit trail
            await Task.Delay(1000);
            await _userService.RevokeSessionAsync(session.SessionToken);

            var revokedSession = await _context.IdentityUserSessions
                .FirstOrDefaultAsync(s => s.Id == session.Id);

            _logger.LogInformation("\nSession revoked:");
            _logger.LogInformation("  IsActive: {IsActive}", revokedSession!.IsActive);
            _logger.LogInformation("  UpdatedAt: {UpdatedAt} (automatically updated)", revokedSession.UpdatedAt);
        }

        _logger.LogInformation("=== End Enterprise Session Management Audit Trail Example ===\n");
    }

    /// <summary>
    /// Demonstrates multi-level permission evaluation with audit trail
    /// </summary>
    public async Task DemonstrateMultiLevelPermissionEvaluationAsync()
    {
        _logger.LogInformation("=== Multi-Level Permission Evaluation with Audit Trail ===");

        // This example shows the 5-level permission hierarchy:
        // Role (0) > Group (50) > User (100) > Model (150) > Object (200)

        var createResult = await _userService.CreateUserAsync(
            "multilevel@example.com",
            "SecurePassword123!",
            "MultiLevel",
            "Demo"
        );

        if (createResult.Succeeded)
        {
            var user = await _userService.GetUserByEmailAsync("multilevel@example.com");

            // Create a role with audit trail
            var role = new IdentityRole("DataAnalyst");
            await _roleManager.CreateAsync(role);
            await _userService.AddToRoleAsync(user!, "DataAnalyst");

            _logger.LogInformation("Role created and assigned: {RoleName}", role.Name);
            _logger.LogInformation("  CreatedAt: {CreatedAt}", role.CreatedAt);

            // Create permissions for different levels
            var readPermission = new IdentityPermission
            {
                Resource = "Data",
                Action = "Read",
                Description = "Permission to read data"
            };

            var writePermission = new IdentityPermission
            {
                Resource = "Data",
                Action = "Write",
                Description = "Permission to write data"
            };

            _context.IdentityPermissions.AddRange(readPermission, writePermission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Permissions created:");
            _logger.LogInformation("  Read Permission - CreatedAt: {CreatedAt}", readPermission.CreatedAt);
            _logger.LogInformation("  Write Permission - CreatedAt: {CreatedAt}", writePermission.CreatedAt);

            // Level 1: Role permission (highest priority)
            await _permissionService.AssignPermissionToRoleAsync(role.Name!, readPermission.Resource, readPermission.Action, isGranted: true);

            // Level 2: Group permission
            var group = new IdentityGroup { Name = "Analysts" };
            _context.IdentityGroups.Add(group);
            await _context.SaveChangesAsync();

            await _userService.AddToGroupAsync(user.Id, group.Id);
            await _permissionService.AssignPermissionToGroupAsync(group.Id, writePermission.Resource, writePermission.Action, isGranted: false);

            // Level 3: User permission
            await _permissionService.AssignPermissionToUserAsync(user.Id, writePermission.Resource, writePermission.Action, isGranted: true);

            _logger.LogInformation("\nMulti-level permissions configured:");
            _logger.LogInformation("  Role: DataAnalyst has READ permission (granted)");
            _logger.LogInformation("  Group: Analysts has WRITE permission (denied)");
            _logger.LogInformation("  User: {UserEmail} has WRITE permission (granted)", user.Email);
            _logger.LogInformation("  Priority evaluation: Role > Group > User");

            // Show audit trail for each permission level
            var rolePermission = await _context.IdentityRolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == readPermission.Id);

            var groupPermission = await _context.IdentityGroupPermissions
                .FirstOrDefaultAsync(gp => gp.GroupId == group.Id && gp.PermissionId == writePermission.Id);

            var userPermission = await _context.IdentityUserPermissions
                .FirstOrDefaultAsync(up => up.UserId == user.Id && up.PermissionId == writePermission.Id);

            _logger.LogInformation("\nAudit trail for permission assignments:");
            _logger.LogInformation("  Role Permission - CreatedAt: {CreatedAt}", rolePermission?.CreatedAt);
            _logger.LogInformation("  Group Permission - CreatedAt: {CreatedAt}", groupPermission?.CreatedAt);
            _logger.LogInformation("  User Permission - CreatedAt: {CreatedAt}", userPermission?.CreatedAt);
        }

        _logger.LogInformation("=== End Multi-Level Permission Evaluation ===\n");
    }

    /// <summary>
    /// Runs all AspNet audit trail examples
    /// </summary>
    public async Task RunAllExamplesAsync()
    {
        _logger.LogInformation("🏢 DIIWO Identity Solution - AspNet Enterprise Audit Trail Demo");
        _logger.LogInformation("====================================================================");

        await DemonstrateAspNetUserAuditTrailAsync();
        await DemonstrateEnterprisePermissionAuditTrailAsync();
        await DemonstrateSessionManagementAuditTrailAsync();
        await DemonstrateMultiLevelPermissionEvaluationAsync();

        _logger.LogInformation("✅ All AspNet enterprise audit trail examples completed successfully!");
        _logger.LogInformation("====================================================================");
    }
}

/// <summary>
/// Console application entry point for running AspNet audit trail examples
/// </summary>
public class AspNetProgram
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection for AspNet Identity
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add Entity Framework with in-memory database for demo
        services.AddDbContext<AspNetIdentityDbContext>(options =>
            options.UseInMemoryDatabase("AspNetAuditTrailDemo"));

        // Add ASP.NET Core Identity
        services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
        })
        .AddEntityFrameworkStores<AspNetIdentityDbContext>()
        .AddDefaultTokenProviders();

        // Add identity services
        services.AddScoped<AspNetUserService>();
        services.AddScoped<AspNetPermissionService>();
        services.AddScoped<AspNetAuditTrailExample>();

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Run examples
        using (var scope = serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AspNetIdentityDbContext>();
            await context.Database.EnsureCreatedAsync();

            var example = scope.ServiceProvider.GetRequiredService<AspNetAuditTrailExample>();
            await example.RunAllExamplesAsync();
        }
    }
}