using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Diiwo.Core.Domain.Enums;
using Diiwo.Identity.App.DbContext;
using Diiwo.Identity.App.Entities;
using Diiwo.Identity.Shared.Enums;
using Diiwo.Identity.Shared.Abstractions.Services;

namespace Diiwo.Identity.App.Application.Services;

/// <summary>
/// APP ARCHITECTURE - Complete 5-level permission evaluation service
/// Handles Role → Group → User → Model → Object permission priority system
/// Priority: Role (0) > Group (50) > User (100) > Model (150) > Object (200)
/// </summary>
public class AppPermissionService : IAppPermissionService
{
    private readonly AppIdentityDbContext _context;
    private readonly ILogger<AppPermissionService> _logger;

    public AppPermissionService(AppIdentityDbContext context, ILogger<AppPermissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Core Permission Check - 5-Level System
    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(Guid userId, string resource, string action, string? modelType = null, Guid? objectId = null, string? objectType = null)
    {
        var user = await _context.Users
            .Include(u => u.UserGroups)
                .ThenInclude(g => g.GroupPermissions)
                    .ThenInclude(gp => gp.Permission)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return false;

        // Get user's roles through many-to-many (simplified - would need role assignment table)
        var userRoles = await GetUserRolesAsync(userId);
        
        var permissions = new List<(bool IsGranted, int Priority)>();

        // Level 1: Role-based permissions (Priority 0 - Highest)
        foreach (var role in userRoles)
        {
            var rolePermissions = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.RoleId == role.Id && 
                           rp.Permission.Resource == resource && 
                           rp.Permission.Action == action)
                .ToListAsync();

            foreach (var rp in rolePermissions)
            {
                permissions.Add((rp.IsGranted, rp.Priority));
            }
        }

        // Level 2: Group-based permissions (Priority 50)
        foreach (var group in user.UserGroups)
        {
            var groupPermissions = group.GroupPermissions
                .Where(gp => gp.Permission.Resource == resource && 
                           gp.Permission.Action == action);

            foreach (var gp in groupPermissions)
            {
                permissions.Add((gp.IsGranted, gp.Priority));
            }
        }

        // Level 3: User-specific permissions (Priority 100)
        var userPermissions = user.UserPermissions
            .Where(up => up.Permission.Resource == resource && 
                       up.Permission.Action == action &&
                       (up.ExpiresAt == null || up.ExpiresAt > DateTime.UtcNow));

        foreach (var up in userPermissions)
        {
            permissions.Add((up.IsGranted, up.Priority));
        }

        // Level 4: Model-level permissions (Priority 150)
        if (!string.IsNullOrEmpty(modelType))
        {
            var modelPermissions = await _context.ModelPermissions
                .Include(mp => mp.Permission)
                .Where(mp => mp.UserId == userId && 
                           mp.Permission.Resource == resource && 
                           mp.Permission.Action == action &&
                           mp.ModelType == modelType)
                .ToListAsync();

            foreach (var mp in modelPermissions)
            {
                permissions.Add((mp.IsGranted, mp.Priority));
            }
        }

        // Level 5: Object-level permissions (Priority 200 - Lowest)
        if (objectId.HasValue && !string.IsNullOrEmpty(objectType))
        {
            var objectPermissions = await _context.ObjectPermissions
                .Include(op => op.Permission)
                .Where(op => op.UserId == userId && 
                           op.Permission.Resource == resource && 
                           op.Permission.Action == action &&
                           op.ObjectId == objectId.Value &&
                           op.ObjectType == objectType)
                .ToListAsync();

            foreach (var op in objectPermissions)
            {
                permissions.Add((op.IsGranted, op.Priority));
            }
        }

        // Evaluate permissions by priority (lower number = higher priority)
        if (!permissions.Any()) return false;

        var highestPriorityPermission = permissions
            .OrderBy(p => p.Priority)
            .First();

        _logger.LogDebug("Permission check - User: {UserId}, Resource: {Resource}, Action: {Action}, Result: {IsGranted}, Priority: {Priority}",
            userId, resource, action, highestPriorityPermission.IsGranted, highestPriorityPermission.Priority);

        return highestPriorityPermission.IsGranted;
    }

    // Simplified permission checks
    /// <inheritdoc />
    public async Task<bool> CanReadAsync(Guid userId, string resource, string? modelType = null, Guid? objectId = null, string? objectType = null)
    {
        return await HasPermissionAsync(userId, resource, "Read", modelType, objectId, objectType);
    }

    /// <inheritdoc />
    public async Task<bool> CanWriteAsync(Guid userId, string resource, string? modelType = null, Guid? objectId = null, string? objectType = null)
    {
        return await HasPermissionAsync(userId, resource, "Write", modelType, objectId, objectType);
    }

    /// <inheritdoc />
    public async Task<bool> CanDeleteAsync(Guid userId, string resource, string? modelType = null, Guid? objectId = null, string? objectType = null)
    {
        return await HasPermissionAsync(userId, resource, "Delete", modelType, objectId, objectType);
    }

    // Permission Management
    /// <inheritdoc />
    public async Task<AppPermission> CreatePermissionAsync(string resource, string action, string? description = null, PermissionScope scope = PermissionScope.Global)
    {
        // Note: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy are set automatically by AuditInterceptor
        var permission = new AppPermission
        {
            Resource = resource,
            Action = action,
            Description = description,
            Scope = scope
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Permission created: {Resource}.{Action} - Scope: {Scope}", resource, action, scope);
        return permission;
    }

    // User Permission Assignment
    /// <inheritdoc />
    public async Task<bool> GrantUserPermissionAsync(Guid userId, Guid permissionId, bool isGranted = true, int priority = 100, DateTime? expiresAt = null)
    {
        var existingPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

        if (existingPermission != null)
        {
            existingPermission.IsGranted = isGranted;
            existingPermission.Priority = priority;
            existingPermission.ExpiresAt = expiresAt;
            // Note: UpdatedAt, UpdatedBy are set automatically by AuditInterceptor
        }
        else
        {
            // Note: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy are set automatically by AuditInterceptor
            var userPermission = new AppUserPermission
            {
                UserId = userId,
                PermissionId = permissionId,
                IsGranted = isGranted,
                Priority = priority,
                ExpiresAt = expiresAt
            };

            _context.UserPermissions.Add(userPermission);
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("User permission {Action}: UserId: {UserId}, PermissionId: {PermissionId}, IsGranted: {IsGranted}",
            existingPermission != null ? "updated" : "granted", userId, permissionId, isGranted);
        
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RevokeUserPermissionAsync(Guid userId, Guid permissionId)
    {
        var userPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

        if (userPermission == null) return false;

        _context.UserPermissions.Remove(userPermission);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User permission revoked: UserId: {UserId}, PermissionId: {PermissionId}", userId, permissionId);
        return true;
    }

    // Group Permission Assignment
    /// <inheritdoc />
    public async Task<bool> GrantGroupPermissionAsync(Guid groupId, Guid permissionId, bool isGranted = true, int priority = 50)
    {
        var existingPermission = await _context.GroupPermissions
            .FirstOrDefaultAsync(gp => gp.GroupId == groupId && gp.PermissionId == permissionId);

        if (existingPermission != null)
        {
            existingPermission.IsGranted = isGranted;
            existingPermission.Priority = priority;
            // Note: UpdatedAt, UpdatedBy are set automatically by AuditInterceptor
        }
        else
        {
            // Note: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy are set automatically by AuditInterceptor
            var groupPermission = new AppGroupPermission
            {
                GroupId = groupId,
                PermissionId = permissionId,
                IsGranted = isGranted,
                Priority = priority
            };

            _context.GroupPermissions.Add(groupPermission);
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Group permission {Action}: GroupId: {GroupId}, PermissionId: {PermissionId}, IsGranted: {IsGranted}",
            existingPermission != null ? "updated" : "granted", groupId, permissionId, isGranted);
        
        return true;
    }

    // Model-Level Permissions
    /// <inheritdoc />
    public async Task<bool> GrantModelPermissionAsync(Guid userId, Guid permissionId, string modelType, bool isGranted = true, int priority = 150)
    {
        var existingPermission = await _context.ModelPermissions
            .FirstOrDefaultAsync(mp => mp.UserId == userId && mp.PermissionId == permissionId && mp.ModelType == modelType);

        if (existingPermission != null)
        {
            existingPermission.IsGranted = isGranted;
            existingPermission.Priority = priority;
            // Note: UpdatedAt, UpdatedBy are set automatically by AuditInterceptor
        }
        else
        {
            // Note: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy are set automatically by AuditInterceptor
            var modelPermission = new AppModelPermission
            {
                UserId = userId,
                PermissionId = permissionId,
                ModelType = modelType,
                IsGranted = isGranted,
                Priority = priority
            };

            _context.ModelPermissions.Add(modelPermission);
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Model permission {Action}: UserId: {UserId}, PermissionId: {PermissionId}, ModelType: {ModelType}, IsGranted: {IsGranted}",
            existingPermission != null ? "updated" : "granted", userId, permissionId, modelType, isGranted);
        
        return true;
    }

    // Object-Level Permissions
    /// <inheritdoc />
    public async Task<bool> GrantObjectPermissionAsync(Guid userId, Guid permissionId, Guid objectId, string objectType, bool isGranted = true, int priority = 200)
    {
        var existingPermission = await _context.ObjectPermissions
            .FirstOrDefaultAsync(op => op.UserId == userId && op.PermissionId == permissionId &&
                                     op.ObjectId == objectId && op.ObjectType == objectType);

        if (existingPermission != null)
        {
            existingPermission.IsGranted = isGranted;
            existingPermission.Priority = priority;
            // Note: UpdatedAt, UpdatedBy are set automatically by AuditInterceptor
        }
        else
        {
            // Note: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy are set automatically by AuditInterceptor
            var objectPermission = new AppObjectPermission
            {
                UserId = userId,
                PermissionId = permissionId,
                ObjectId = objectId,
                ObjectType = objectType,
                IsGranted = isGranted,
                Priority = priority
            };

            _context.ObjectPermissions.Add(objectPermission);
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Object permission {Action}: UserId: {UserId}, PermissionId: {PermissionId}, ObjectId: {ObjectId}, ObjectType: {ObjectType}, IsGranted: {IsGranted}",
            existingPermission != null ? "updated" : "granted", userId, permissionId, objectId, objectType, isGranted);
        
        return true;
    }

    // Helper Methods
    /// <inheritdoc />
    public async Task<List<AppPermission>> GetUserEffectivePermissionsAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.UserGroups)
                .ThenInclude(g => g.GroupPermissions)
                    .ThenInclude(gp => gp.Permission)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return new List<AppPermission>();

        var permissions = new HashSet<AppPermission>();

        // Collect from groups
        foreach (var group in user.UserGroups)
        {
            foreach (var gp in group.GroupPermissions.Where(gp => gp.IsGranted))
            {
                permissions.Add(gp.Permission);
            }
        }

        // Collect from user permissions
        foreach (var up in user.UserPermissions.Where(up => up.IsGranted && (up.ExpiresAt == null || up.ExpiresAt > DateTime.UtcNow)))
        {
            permissions.Add(up.Permission);
        }

        return permissions.ToList();
    }

    private Task<List<AppRole>> GetUserRolesAsync(Guid userId)
    {
        // This would need to be implemented based on your role assignment logic
        // For now, returning empty list since we don't have a direct role assignment table
        return Task.FromResult(new List<AppRole>());
    }

    // Implement interface methods that match the expected signatures
    /// <inheritdoc />
    public async Task<bool> UserHasPermissionAsync(Guid userId, string resource, string action)
    {
        return await HasPermissionAsync(userId, resource, action);
    }

    /// <inheritdoc />
    public async Task<bool> UserHasModelPermissionAsync(Guid userId, string resource, string action, string modelType)
    {
        return await HasPermissionAsync(userId, resource, action, modelType);
    }

    /// <inheritdoc />
    public async Task<bool> UserHasObjectPermissionAsync(Guid userId, string resource, string action, Guid objectId, string objectType)
    {
        return await HasPermissionAsync(userId, resource, action, null, objectId, objectType);
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, bool>> UserHasPermissionsAsync(Guid userId, params string[] permissions)
    {
        var result = new Dictionary<string, bool>();
        foreach (var permission in permissions)
        {
            var parts = permission.Split('.');
            if (parts.Length == 2)
            {
                var hasPermission = await HasPermissionAsync(userId, parts[0], parts[1]);
                result[permission] = hasPermission;
            }
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> GrantUserPermissionAsync(Guid userId, string resource, string action, DateTime? expiresAt = null, int priority = 100, Guid? grantedBy = null)
    {
        var permission = await _context.Permissions
            .FirstOrDefaultAsync(p => p.Resource == resource && p.Action == action);

        if (permission == null)
        {
            permission = await CreatePermissionAsync(resource, action);
        }

        return await GrantUserPermissionAsync(userId, permission.Id, true, priority, expiresAt);
    }

    /// <inheritdoc />
    public async Task<bool> RevokeUserPermissionAsync(Guid userId, string resource, string action, Guid? revokedBy = null)
    {
        var permission = await _context.Permissions
            .FirstOrDefaultAsync(p => p.Resource == resource && p.Action == action);

        if (permission == null) return false;

        return await RevokeUserPermissionAsync(userId, permission.Id);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetUserPermissionsAsync(Guid userId)
    {
        var permissions = await GetUserEffectivePermissionsAsync(userId);
        return permissions.Select(p => $"{p.Resource}.{p.Action}").ToList();
    }

    /// <inheritdoc />
    public async Task<bool> RoleHasPermissionAsync(Guid roleId, string resource, string action)
    {
        var permission = await _context.Permissions
            .FirstOrDefaultAsync(p => p.Resource == resource && p.Action == action);

        if (permission == null) return false;

        return await _context.RolePermissions
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id && rp.IsGranted);
    }

    /// <inheritdoc />
    public async Task<bool> GroupHasPermissionAsync(Guid groupId, string resource, string action)
    {
        var permission = await _context.Permissions
            .FirstOrDefaultAsync(p => p.Resource == resource && p.Action == action);

        if (permission == null) return false;

        return await _context.GroupPermissions
            .AnyAsync(gp => gp.GroupId == groupId && gp.PermissionId == permission.Id && gp.IsGranted);
    }
}