using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Diiwo.Identity.App.DbContext;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.Migration.Models;
using Diiwo.Identity.Migration.Abstractions.Services;
using EFEntityState = Microsoft.EntityFrameworkCore.EntityState;

namespace Diiwo.Identity.Migration.Services;

/// <summary>
/// MIGRATION SERVICE - Migrates data from App architecture to AspNet architecture
/// Handles complete data migration with foreign key relationships
/// Maintains data integrity and provides rollback capabilities
/// </summary>
public class AppToAspNetMigrationService : IAppToAspNetMigrationService
{
    private readonly AppIdentityDbContext _appContext;
    private readonly AspNetIdentityDbContext _aspNetContext;
    private readonly Microsoft.AspNetCore.Identity.UserManager<IdentityUser> _userManager;
    private readonly Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AppToAspNetMigrationService> _logger;

    public AppToAspNetMigrationService(
        AppIdentityDbContext appContext,
        AspNetIdentityDbContext aspNetContext,
        Microsoft.AspNetCore.Identity.UserManager<IdentityUser> userManager,
        Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> roleManager,
        ILogger<AppToAspNetMigrationService> logger)
    {
        _appContext = appContext;
        _aspNetContext = aspNetContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MigrationResult> MigrateAllAsync(bool createForeignKeyRelationships = true, bool preserveAppData = true)
    {
        var result = new MigrationResult { StartedAt = DateTime.UtcNow };
        
        try
        {
            _logger.LogInformation("Starting complete migration from App to AspNet architecture");

            using var transaction = await _aspNetContext.Database.BeginTransactionAsync();
            
            try
            {
                // Step 1: Migrate Permissions
                var permissionMappings = await MigratePermissionsAsync();
                result.PermissionsMigrated = permissionMappings.Count;

                // Step 2: Migrate Roles
                var roleMappings = await MigrateRolesAsync();
                result.RolesMigrated = roleMappings.Count;

                // Step 3: Migrate Users
                var userMappings = await MigrateUsersInternalAsync(createForeignKeyRelationships);
                result.UsersMigrated = userMappings.Count;

                // Step 4: Migrate Groups
                var groupMappings = await MigrateGroupsAsync(createForeignKeyRelationships);
                result.GroupsMigrated = groupMappings.Count;

                // Step 5: Migrate Role Permissions
                await MigrateRolePermissionsAsync(roleMappings, permissionMappings);

                // Step 6: Migrate Group Permissions
                await MigrateGroupPermissionsAsync(groupMappings, permissionMappings);

                // Step 7: Migrate User Permissions
                await MigrateUserPermissionsAsync(userMappings, permissionMappings);

                // Step 8: Migrate Model Permissions
                await MigrateModelPermissionsAsync(userMappings, permissionMappings);

                // Step 9: Migrate Object Permissions
                await MigrateObjectPermissionsAsync(userMappings, permissionMappings);

                // Step 10: Migrate User Sessions
                await MigrateUserSessionsAsync(userMappings);

                // Step 11: Migrate Login History
                await MigrateLoginHistoryAsync(userMappings);

                // Step 12: Setup User-Group relationships
                await MigrateUserGroupMembershipsAsync(userMappings, groupMappings);

                await transaction.CommitAsync();

                result.IsSuccessful = true;
                result.CompletedAt = DateTime.UtcNow;
                
                _logger.LogInformation("Migration completed successfully. Users: {UserCount}, Roles: {RoleCount}, Groups: {GroupCount}, Permissions: {PermissionCount}",
                    result.UsersMigrated, result.RolesMigrated, result.GroupsMigrated, result.PermissionsMigrated);

                // Optional: Clean up App data if not preserving
                if (!preserveAppData)
                {
                    await CleanupAppDataAsync();
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            result.CompletedAt = DateTime.UtcNow;
            
            _logger.LogError(ex, "Migration failed: {ErrorMessage}", ex.Message);
        }

        return result;
    }

    private async Task<Dictionary<Guid, Guid>> MigratePermissionsAsync()
    {
        _logger.LogInformation("Migrating permissions...");
        
        var appPermissions = await _appContext.Permissions.ToListAsync();
        var mappings = new Dictionary<Guid, Guid>();

        foreach (var appPermission in appPermissions)
        {
            var identityPermission = new IdentityPermission
            {
                Resource = appPermission.Resource,
                Action = appPermission.Action,
                Description = appPermission.Description,
                Scope = appPermission.Scope,
                CreatedAt = appPermission.CreatedAt,
                UpdatedAt = appPermission.UpdatedAt,
                AppPermissionId = appPermission.Id // Foreign key relationship
            };

            _aspNetContext.IdentityPermissions.Add(identityPermission);
            mappings[appPermission.Id] = identityPermission.Id;
        }

        await _aspNetContext.SaveChangesAsync();
        return mappings;
    }

    private async Task<Dictionary<Guid, Guid>> MigrateRolesAsync()
    {
        _logger.LogInformation("Migrating roles...");
        
        var appRoles = await _appContext.Roles.ToListAsync();
        var mappings = new Dictionary<Guid, Guid>();

        foreach (var appRole in appRoles)
        {
            var identityRole = new IdentityRole
            {
                Name = appRole.Name,
                NormalizedName = appRole.Name.ToUpperInvariant(),
                Description = appRole.Description,
                CreatedAt = appRole.CreatedAt,
                UpdatedAt = appRole.UpdatedAt,
                AppRoleId = appRole.Id, // Foreign key relationship
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            var result = await _roleManager.CreateAsync(identityRole);
            if (result.Succeeded)
            {
                mappings[appRole.Id] = identityRole.Id;
            }
            else
            {
                _logger.LogWarning("Failed to create role {RoleName}: {Errors}", 
                    appRole.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        return mappings;
    }

    /// <inheritdoc />
    public async Task<MigrationResult> MigrateUsersAsync(bool createForeignKeyRelationships = true)
    {
        var userMappings = await MigrateUsersInternalAsync(createForeignKeyRelationships);
        
        var migrationResult = new MigrationResult 
        { 
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            IsSuccessful = true,
            UsersMigrated = userMappings.Count
        };
        
        return migrationResult;
    }

    private async Task<Dictionary<Guid, Guid>> MigrateUsersInternalAsync(bool createForeignKeyRelationships = true)
    {
        _logger.LogInformation("Migrating users...");
        
        var userMappings = new Dictionary<Guid, Guid>();
        var appUsers = await _appContext.Users.ToListAsync();
        
        foreach (var appUser in appUsers)
        {
            var identityUser = new IdentityUser
            {
                UserName = appUser.Username ?? appUser.Email,
                Email = appUser.Email,
                NormalizedUserName = (appUser.Username ?? appUser.Email).ToUpperInvariant(),
                NormalizedEmail = appUser.Email.ToUpperInvariant(),
                EmailConfirmed = appUser.EmailConfirmed,
                PasswordHash = appUser.PasswordHash,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                PhoneNumber = appUser.PhoneNumber,
                TwoFactorEnabled = false, // Default false
                LockoutEnabled = true,
                AccessFailedCount = 0,
                FirstName = appUser.FirstName,
                LastName = appUser.LastName,
                LastLoginAt = appUser.LastLoginAt,
                LastLoginIp = appUser.LastLoginIp,
                TwoFactorSecret = appUser.TwoFactorSecret,
                CreatedAt = appUser.CreatedAt,
                UpdatedAt = appUser.UpdatedAt
            };

            if (createForeignKeyRelationships)
            {
                identityUser.AppUserId = appUser.Id;
            }

            var result = await _userManager.CreateAsync(identityUser);
            if (result.Succeeded)
            {
                userMappings[appUser.Id] = identityUser.Id;
            }
            else
            {
                _logger.LogWarning("Failed to create user {Email}: {Errors}", 
                    appUser.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        _logger.LogInformation("User migration completed. Migrated {UserCount} users", userMappings.Count);
        return userMappings;
    }

    private async Task<Dictionary<Guid, Guid>> MigrateGroupsAsync(bool createForeignKeyRelationships)
    {
        _logger.LogInformation("Migrating groups...");
        
        var appGroups = await _appContext.Groups.ToListAsync();
        var mappings = new Dictionary<Guid, Guid>();

        foreach (var appGroup in appGroups)
        {
            var identityGroup = new IdentityGroup
            {
                Name = appGroup.Name,
                Description = appGroup.Description,
                CreatedAt = appGroup.CreatedAt,
                UpdatedAt = appGroup.UpdatedAt
            };

            if (createForeignKeyRelationships)
            {
                identityGroup.AppGroupId = appGroup.Id; // Foreign key relationship
            }

            _aspNetContext.IdentityGroups.Add(identityGroup);
            mappings[appGroup.Id] = identityGroup.Id;
        }

        await _aspNetContext.SaveChangesAsync();
        return mappings;
    }

    private async Task MigrateRolePermissionsAsync(Dictionary<Guid, Guid> roleMappings, Dictionary<Guid, Guid> permissionMappings)
    {
        _logger.LogInformation("Migrating role permissions...");
        
        var appRolePermissions = await _appContext.RolePermissions.ToListAsync();

        foreach (var appRolePermission in appRolePermissions)
        {
            if (roleMappings.TryGetValue(appRolePermission.RoleId, out var identityRoleId) &&
                permissionMappings.TryGetValue(appRolePermission.PermissionId, out var identityPermissionId))
            {
                var identityRolePermission = new IdentityRolePermission
                {
                    RoleId = identityRoleId,
                    PermissionId = identityPermissionId,
                    IsGranted = appRolePermission.IsGranted,
                    Priority = appRolePermission.Priority,
                    CreatedAt = appRolePermission.CreatedAt,
                    UpdatedAt = appRolePermission.UpdatedAt
                };

                _aspNetContext.IdentityRolePermissions.Add(identityRolePermission);
            }
        }

        await _aspNetContext.SaveChangesAsync();
    }

    private async Task MigrateGroupPermissionsAsync(Dictionary<Guid, Guid> groupMappings, Dictionary<Guid, Guid> permissionMappings)
    {
        _logger.LogInformation("Migrating group permissions...");
        
        var appGroupPermissions = await _appContext.GroupPermissions.ToListAsync();

        foreach (var appGroupPermission in appGroupPermissions)
        {
            if (groupMappings.TryGetValue(appGroupPermission.GroupId, out var identityGroupId) &&
                permissionMappings.TryGetValue(appGroupPermission.PermissionId, out var identityPermissionId))
            {
                var identityGroupPermission = new IdentityGroupPermission
                {
                    GroupId = identityGroupId,
                    PermissionId = identityPermissionId,
                    IsGranted = appGroupPermission.IsGranted,
                    Priority = appGroupPermission.Priority,
                    CreatedAt = appGroupPermission.CreatedAt,
                    UpdatedAt = appGroupPermission.UpdatedAt
                };

                _aspNetContext.IdentityGroupPermissions.Add(identityGroupPermission);
            }
        }

        await _aspNetContext.SaveChangesAsync();
    }

    private async Task MigrateUserPermissionsAsync(Dictionary<Guid, Guid> userMappings, Dictionary<Guid, Guid> permissionMappings)
    {
        _logger.LogInformation("Migrating user permissions...");
        
        var appUserPermissions = await _appContext.UserPermissions.ToListAsync();

        foreach (var appUserPermission in appUserPermissions)
        {
            if (userMappings.TryGetValue(appUserPermission.UserId, out var identityUserId) &&
                permissionMappings.TryGetValue(appUserPermission.PermissionId, out var identityPermissionId))
            {
                var identityUserPermission = new IdentityUserPermission
                {
                    UserId = identityUserId,
                    PermissionId = identityPermissionId,
                    IsGranted = appUserPermission.IsGranted,
                    Priority = appUserPermission.Priority,
                    ExpiresAt = appUserPermission.ExpiresAt,
                    CreatedAt = appUserPermission.CreatedAt,
                    UpdatedAt = appUserPermission.UpdatedAt
                };

                _aspNetContext.IdentityUserPermissions.Add(identityUserPermission);
            }
        }

        await _aspNetContext.SaveChangesAsync();
    }

    private async Task MigrateModelPermissionsAsync(Dictionary<Guid, Guid> userMappings, Dictionary<Guid, Guid> permissionMappings)
    {
        _logger.LogInformation("Migrating model permissions...");
        
        var appModelPermissions = await _appContext.ModelPermissions.ToListAsync();

        foreach (var appModelPermission in appModelPermissions)
        {
            if (userMappings.TryGetValue(appModelPermission.UserId, out var identityUserId) &&
                permissionMappings.TryGetValue(appModelPermission.PermissionId, out var identityPermissionId))
            {
                var identityModelPermission = new IdentityModelPermission
                {
                    UserId = identityUserId,
                    PermissionId = identityPermissionId,
                    ModelType = appModelPermission.ModelType,
                    IsGranted = appModelPermission.IsGranted,
                    Priority = appModelPermission.Priority,
                    CreatedAt = appModelPermission.CreatedAt,
                    UpdatedAt = appModelPermission.UpdatedAt
                };

                _aspNetContext.IdentityModelPermissions.Add(identityModelPermission);
            }
        }

        await _aspNetContext.SaveChangesAsync();
    }

    private async Task MigrateObjectPermissionsAsync(Dictionary<Guid, Guid> userMappings, Dictionary<Guid, Guid> permissionMappings)
    {
        _logger.LogInformation("Migrating object permissions...");
        
        var appObjectPermissions = await _appContext.ObjectPermissions.ToListAsync();

        foreach (var appObjectPermission in appObjectPermissions)
        {
            if (userMappings.TryGetValue(appObjectPermission.UserId, out var identityUserId) &&
                permissionMappings.TryGetValue(appObjectPermission.PermissionId, out var identityPermissionId))
            {
                var identityObjectPermission = new IdentityObjectPermission
                {
                    UserId = identityUserId,
                    PermissionId = identityPermissionId,
                    ObjectId = appObjectPermission.ObjectId,
                    ObjectType = appObjectPermission.ObjectType,
                    IsGranted = appObjectPermission.IsGranted,
                    Priority = appObjectPermission.Priority,
                    CreatedAt = appObjectPermission.CreatedAt,
                    UpdatedAt = appObjectPermission.UpdatedAt
                };

                _aspNetContext.IdentityObjectPermissions.Add(identityObjectPermission);
            }
        }

        await _aspNetContext.SaveChangesAsync();
    }

    private async Task MigrateUserSessionsAsync(Dictionary<Guid, Guid> userMappings)
    {
        _logger.LogInformation("Migrating user sessions...");
        
        var appUserSessions = await _appContext.UserSessions.ToListAsync();

        foreach (var appUserSession in appUserSessions)
        {
            if (userMappings.TryGetValue(appUserSession.UserId, out var identityUserId))
            {
                var identityUserSession = new IdentityUserSession
                {
                    UserId = identityUserId,
                    SessionToken = appUserSession.SessionToken,
                    IpAddress = appUserSession.IpAddress,
                    UserAgent = appUserSession.UserAgent,
                    SessionType = appUserSession.SessionType,
                    IsActive = appUserSession.IsActive,
                    ExpiresAt = appUserSession.ExpiresAt,
                    CreatedAt = appUserSession.CreatedAt,
                    UpdatedAt = appUserSession.UpdatedAt
                };

                _aspNetContext.IdentityUserSessions.Add(identityUserSession);
            }
        }

        await _aspNetContext.SaveChangesAsync();
    }

    private async Task MigrateLoginHistoryAsync(Dictionary<Guid, Guid> userMappings)
    {
        _logger.LogInformation("Migrating login history...");
        
        var appLoginHistory = await _appContext.LoginHistory.ToListAsync();

        foreach (var appLogin in appLoginHistory)
        {
            if (userMappings.TryGetValue(appLogin.UserId, out var identityUserId))
            {
                var identityLoginHistory = new IdentityLoginHistory
                {
                    UserId = identityUserId,
                    IsSuccessful = appLogin.IsSuccessful,
                    IpAddress = appLogin.IpAddress,
                    UserAgent = appLogin.UserAgent,
                    FailureReason = appLogin.FailureReason,
                    AuthMethod = appLogin.AuthMethod,
                    LoginAttemptAt = appLogin.LoginAttemptAt,
                    CreatedAt = appLogin.CreatedAt,
                    UpdatedAt = appLogin.UpdatedAt
                };

                _aspNetContext.IdentityLoginHistory.Add(identityLoginHistory);
            }
        }

        await _aspNetContext.SaveChangesAsync();
    }

    private async Task MigrateUserGroupMembershipsAsync(Dictionary<Guid, Guid> userMappings, Dictionary<Guid, Guid> groupMappings)
    {
        _logger.LogInformation("Migrating user-group memberships...");
        
        var appUsers = await _appContext.Users
            .Include(u => u.UserGroups)
            .ToListAsync();

        foreach (var appUser in appUsers)
        {
            if (userMappings.TryGetValue(appUser.Id, out var identityUserId))
            {
                var identityUser = await _aspNetContext.Users
                    .Include(u => u.IdentityUserGroups)
                    .FirstOrDefaultAsync(u => u.Id == identityUserId);

                if (identityUser != null)
                {
                    foreach (var appGroup in appUser.UserGroups)
                    {
                        if (groupMappings.TryGetValue(appGroup.Id, out var identityGroupId))
                        {
                            var identityGroup = await _aspNetContext.IdentityGroups.FindAsync(identityGroupId);
                            if (identityGroup != null)
                            {
                                identityUser.IdentityUserGroups.Add(identityGroup);
                            }
                        }
                    }
                }
            }
        }

        await _aspNetContext.SaveChangesAsync();
    }

    private async Task CleanupAppDataAsync()
    {
        _logger.LogInformation("Cleaning up App architecture data...");
        
        // This would remove all data from App tables
        // Implementation depends on business requirements
        // For now, just log that cleanup would happen here
        
        _logger.LogWarning("App data cleanup not implemented - data preservation enabled by default");
    }

    /// <inheritdoc />
    public async Task<MigrationValidationResult> ValidateMigrationAsync()
    {
        var result = new MigrationValidationResult();
        
        try
        {
            // Count records in both systems
            result.AppUsers = await _appContext.Users.CountAsync();
            result.AspNetUsers = await _aspNetContext.Users.CountAsync();
            
            result.AppRoles = await _appContext.Roles.CountAsync();
            result.AspNetRoles = await _aspNetContext.Roles.CountAsync();
            
            result.AppPermissions = await _appContext.Permissions.CountAsync();
            result.AspNetPermissions = await _aspNetContext.IdentityPermissions.CountAsync();
            
            result.AppGroups = await _appContext.Groups.CountAsync();
            result.AspNetGroups = await _aspNetContext.IdentityGroups.CountAsync();

            // Check for discrepancies
            result.IsValid = result.AppUsers == result.AspNetUsers &&
                           result.AppRoles == result.AspNetRoles &&
                           result.AppPermissions == result.AspNetPermissions &&
                           result.AppGroups == result.AspNetGroups;

            _logger.LogInformation("Migration validation - Users: {AppUsers}/{AspNetUsers}, Roles: {AppRoles}/{AspNetRoles}, Permissions: {AppPermissions}/{AspNetPermissions}, Groups: {AppGroups}/{AspNetGroups}",
                result.AppUsers, result.AspNetUsers, result.AppRoles, result.AspNetRoles, 
                result.AppPermissions, result.AspNetPermissions, result.AppGroups, result.AspNetGroups);
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ValidationErrors.Add($"Validation failed: {ex.Message}");
        }

        return result;
    }
}