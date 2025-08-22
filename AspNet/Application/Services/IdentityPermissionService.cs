using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.Shared.Abstractions.Services;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.AspNet.Application.Services;

/// <summary>
///  ASPNET ARCHITECTURE - Identity Permission Service Implementation
/// Complete implementation of IIdentityPermissionService for ASP.NET Core Identity
/// Provides 5-level permission checking with enterprise features
/// </summary>
public class IdentityPermissionService : IIdentityPermissionService
{
    private readonly AspNetIdentityDbContext _context;
    private readonly Microsoft.AspNetCore.Identity.UserManager<IdentityUser> _userManager;
    private readonly Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<IdentityPermissionService> _logger;

    public IdentityPermissionService(
        AspNetIdentityDbContext context,
        Microsoft.AspNetCore.Identity.UserManager<IdentityUser> userManager,
        Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> roleManager,
        ILogger<IdentityPermissionService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    #region IIdentityPermissionService Implementation

    /// <inheritdoc />
    public async Task<bool> UserHasPermissionAsync(Guid userId, string permissionName)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        var (resource, action) = ParsePermissionName(permissionName);
        return await CheckUserPermissionAsync(userId, resource, action);
    }

    /// <inheritdoc />
    public async Task<bool> UserHasPermissionAsync(string userEmail, string permissionName)
    {
        var user = await _userManager.FindByEmailAsync(userEmail);
        if (user == null) return false;

        var (resource, action) = ParsePermissionName(permissionName);
        return await CheckUserPermissionAsync(user.Id, resource, action);
    }

    /// <inheritdoc />
    public async Task<bool> RoleHasPermissionAsync(Guid roleId, string permissionName)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null) return false;

        var (resource, action) = ParsePermissionName(permissionName);
        
        var permission = await GetPermissionAsync(resource, action);
        if (permission == null) return false;

        var rolePermission = await _context.IdentityRolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id);

        return rolePermission?.IsGranted ?? false;
    }

    /// <inheritdoc />
    public async Task<bool> RoleHasPermissionAsync(string roleName, string permissionName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null) return false;

        return await RoleHasPermissionAsync(role.Id, permissionName);
    }

    /// <inheritdoc />
    public async Task<bool> UserHasModelPermissionAsync(Guid userId, string permissionName, string modelType)
    {
        // First check base permission
        if (!await UserHasPermissionAsync(userId, permissionName)) return false;

        var (resource, action) = ParsePermissionName(permissionName);
        var permission = await GetPermissionAsync(resource, action);
        if (permission == null) return false;

        // Check model-specific permission
        var modelPermission = await _context.IdentityModelPermissions
            .FirstOrDefaultAsync(mp => mp.UserId == userId && 
                                    mp.PermissionId == permission.Id &&
                                    mp.ModelType == modelType);

        return modelPermission?.IsGranted ?? true; // Default allow if no specific restriction
    }

    /// <inheritdoc />
    public async Task<bool> UserHasObjectPermissionAsync(Guid userId, string permissionName, Guid objectId, string objectType)
    {
        // First check base permission
        if (!await UserHasPermissionAsync(userId, permissionName)) return false;

        var (resource, action) = ParsePermissionName(permissionName);
        var permission = await GetPermissionAsync(resource, action);
        if (permission == null) return false;

        // Check object-specific permission
        var objectPermission = await _context.IdentityObjectPermissions
            .FirstOrDefaultAsync(op => op.UserId == userId && 
                                     op.PermissionId == permission.Id &&
                                     op.ObjectId == objectId &&
                                     op.ObjectType == objectType);

        return objectPermission?.IsGranted ?? true; // Default allow if no specific restriction
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, bool>> UserHasPermissionsAsync(Guid userId, params string[] permissionNames)
    {
        var results = new Dictionary<string, bool>();

        foreach (var permissionName in permissionNames)
        {
            results[permissionName] = await UserHasPermissionAsync(userId, permissionName);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<bool> GrantUserPermissionAsync(Guid userId, string permissionName, DateTime? expiresAt = null, int priority = 100, Guid? grantedBy = null)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            var (resource, action) = ParsePermissionName(permissionName);
            var permission = await GetOrCreatePermissionAsync(resource, action);

            // Check if permission already exists
            var existingPermission = await _context.IdentityUserPermissions
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permission.Id);

            if (existingPermission != null)
            {
                // Update existing permission
                existingPermission.IsGranted = true;
                existingPermission.Priority = priority;
                existingPermission.ExpiresAt = expiresAt;
                existingPermission.UpdatedAt = DateTime.UtcNow;
                existingPermission.UpdatedBy = grantedBy;
            }
            else
            {
                // Create new permission
                var userPermission = new IdentityUserPermission
                {
                    UserId = userId,
                    PermissionId = permission.Id,
                    IsGranted = true,
                    Priority = priority,
                    ExpiresAt = expiresAt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = grantedBy,
                    UpdatedBy = grantedBy
                };

                _context.IdentityUserPermissions.Add(userPermission);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Permission '{PermissionName}' granted to user {UserId} by {GrantedBy}", 
                permissionName, userId, grantedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant permission '{PermissionName}' to user {UserId}", 
                permissionName, userId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RevokeUserPermissionAsync(Guid userId, string permissionName, Guid? revokedBy = null)
    {
        try
        {
            var (resource, action) = ParsePermissionName(permissionName);
            var permission = await GetPermissionAsync(resource, action);
            if (permission == null) return false;

            var userPermission = await _context.IdentityUserPermissions
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permission.Id);

            if (userPermission == null) return false;

            // Mark as revoked (not granted)
            userPermission.IsGranted = false;
            userPermission.UpdatedAt = DateTime.UtcNow;
            userPermission.UpdatedBy = revokedBy;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Permission '{PermissionName}' revoked from user {UserId} by {RevokedBy}", 
                permissionName, userId, revokedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke permission '{PermissionName}' from user {UserId}", 
                permissionName, userId);
            return false;
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<bool> CheckUserPermissionAsync(Guid userId, string resource, string action)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        // 1. Check Role-level permissions (highest priority)
        var rolePermissions = await GetUserRolePermissionsAsync(userId, resource, action);
        var denyRole = rolePermissions.Where(p => !p.IsGranted).OrderByDescending(p => p.Priority).FirstOrDefault();
        if (denyRole != null) return false;

        var grantRole = rolePermissions.Where(p => p.IsGranted).OrderByDescending(p => p.Priority).FirstOrDefault();
        if (grantRole != null) return true;

        // 2. Check Group-level permissions
        var groupPermissions = await GetUserGroupPermissionsAsync(userId, resource, action);
        var denyGroup = groupPermissions.Where(p => !p.IsGranted).OrderByDescending(p => p.Priority).FirstOrDefault();
        if (denyGroup != null) return false;

        var grantGroup = groupPermissions.Where(p => p.IsGranted).OrderByDescending(p => p.Priority).FirstOrDefault();
        if (grantGroup != null) return true;

        // 3. Check User-level permissions (direct assignments)
        var userPermission = await GetDirectUserPermissionAsync(userId, resource, action);
        if (userPermission != null) 
        {
            // Check if permission has expired
            if (userPermission.ExpiresAt.HasValue && userPermission.ExpiresAt.Value <= DateTime.UtcNow)
                return false;
                
            return userPermission.IsGranted;
        }

        return false; // Default deny
    }

    private async Task<List<IdentityRolePermission>> GetUserRolePermissionsAsync(Guid userId, string resource, string action)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return new List<IdentityRolePermission>();

        var userRoles = await _userManager.GetRolesAsync(user);
        
        return await _context.IdentityRolePermissions
            .Include(rp => rp.Permission)
            .Include(rp => rp.Role)
            .Where(rp => userRoles.Contains(rp.Role.Name!) && 
                        rp.Permission.Resource == resource && 
                        rp.Permission.Action == action &&
                        rp.Permission.IsActive)
            .ToListAsync();
    }

    private async Task<List<IdentityGroupPermission>> GetUserGroupPermissionsAsync(Guid userId, string resource, string action)
    {
        var user = await _context.Users
            .Include(u => u.IdentityUserGroups)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return new List<IdentityGroupPermission>();

        var userGroupIds = user.IdentityUserGroups.Select(g => g.Id).ToList();

        return await _context.IdentityGroupPermissions
            .Include(gp => gp.Permission)
            .Where(gp => userGroupIds.Contains(gp.GroupId) && 
                        gp.Permission.Resource == resource && 
                        gp.Permission.Action == action &&
                        gp.Permission.IsActive)
            .ToListAsync();
    }

    private async Task<IdentityUserPermission?> GetDirectUserPermissionAsync(Guid userId, string resource, string action)
    {
        return await _context.IdentityUserPermissions
            .Include(up => up.Permission)
            .FirstOrDefaultAsync(up => up.UserId == userId && 
                                     up.Permission.Resource == resource && 
                                     up.Permission.Action == action &&
                                     up.Permission.IsActive);
    }

    private async Task<IdentityPermission?> GetPermissionAsync(string resource, string action)
    {
        return await _context.IdentityPermissions
            .FirstOrDefaultAsync(p => p.Resource == resource && p.Action == action && p.IsActive);
    }

    private async Task<IdentityPermission> GetOrCreatePermissionAsync(string resource, string action)
    {
        var permission = await GetPermissionAsync(resource, action);
        
        if (permission == null)
        {
            permission = new IdentityPermission
            {
                Resource = resource,
                Action = action,
                Description = $"Auto-created permission for {resource}.{action}",
                Scope = PermissionScope.Global,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.IdentityPermissions.Add(permission);
            await _context.SaveChangesAsync();
        }

        return permission;
    }

    private static (string resource, string action) ParsePermissionName(string permissionName)
    {
        var parts = permissionName.Split('.');
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Permission name '{permissionName}' must be in format 'Resource.Action'", nameof(permissionName));
        }

        return (parts[0], parts[1]);
    }

    #endregion
}