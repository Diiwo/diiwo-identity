using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.AspNet.Abstractions.Services;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.AspNet.Application.Services;

/// <summary>
///  ASPNET ARCHITECTURE - Enterprise permission management service
/// Full integration with ASP.NET Core Identity with 5-level permission system
/// Handles Role → Group → User → Model → Object permission hierarchy
/// </summary>
public class AspNetPermissionService : IAspNetPermissionService
{
    private readonly AspNetIdentityDbContext _context;
    private readonly Microsoft.AspNetCore.Identity.UserManager<IdentityUser> _userManager;
    private readonly Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AspNetPermissionService> _logger;

    public AspNetPermissionService(
        AspNetIdentityDbContext context,
        Microsoft.AspNetCore.Identity.UserManager<IdentityUser> userManager,
        Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> roleManager,
        ILogger<AspNetPermissionService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    // Permission Management
    /// <inheritdoc />
    public async Task<IdentityPermission> CreatePermissionAsync(string resource, string action, string? description = null, PermissionScope scope = PermissionScope.Global)
    {
        var permission = new IdentityPermission
        {
            Resource = resource,
            Action = action,
            Description = description,
            Scope = scope
        };

        _context.IdentityPermissions.Add(permission);
        await _context.SaveChangesAsync();

        return permission;
    }

    /// <inheritdoc />
    public async Task<List<IdentityPermission>> GetAllPermissionsAsync()
    {
        return await _context.IdentityPermissions
            .Where(p => p.IsActive)
            .OrderBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IdentityPermission?> GetPermissionAsync(string resource, string action)
    {
        return await _context.IdentityPermissions
            .FirstOrDefaultAsync(p => p.Resource == resource && p.Action == action && p.IsActive);
    }

    // 5-Level Permission Check: Role → Group → User → Model → Object
    // SECURITY PRINCIPLE: DENY ALWAYS WINS - Any explicit deny overrides all grants
    /// <inheritdoc />
    public async Task<bool> UserHasPermissionAsync(Guid userId, string resource, string action)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        // Collect all permissions from all levels
        var rolePermissions = await GetUserRolePermissionsAsync(userId, resource, action);
        var groupPermissions = await GetUserGroupPermissionsAsync(userId, resource, action);
        var userPermission = await GetDirectUserPermissionAsync(userId, resource, action);

        // SECURITY RULE: Check for ANY explicit DENY first - DENY always wins
        // Check role denies
        if (rolePermissions.Any(p => !p.IsGranted)) return false;

        // Check group denies
        if (groupPermissions.Any(p => !p.IsGranted)) return false;

        // Check user deny
        if (userPermission != null && !userPermission.IsGranted) return false;

        // If no denies found, check for grants in priority order
        // 1. Check Role-level grants (highest priority)
        var grantRole = rolePermissions.Where(p => p.IsGranted).OrderByDescending(p => p.Priority).FirstOrDefault();
        if (grantRole != null) return true;

        // 2. Check Group-level grants
        var grantGroup = groupPermissions.Where(p => p.IsGranted).OrderByDescending(p => p.Priority).FirstOrDefault();
        if (grantGroup != null) return true;

        // 3. Check User-level grants (direct assignments)
        if (userPermission != null && userPermission.IsGranted) return true;

        return false; // Default deny
    }

    /// <inheritdoc />
    public async Task<bool> UserHasModelPermissionAsync(Guid userId, string resource, string action, string modelType)
    {
        // First check base permission
        if (!await UserHasPermissionAsync(userId, resource, action)) return false;

        // Then check model-specific permission
        var modelPermission = await _context.IdentityModelPermissions
            .Include(mp => mp.Permission)
            .FirstOrDefaultAsync(mp => mp.UserId == userId && 
                                    mp.Permission.Resource == resource && 
                                    mp.Permission.Action == action &&
                                    mp.ModelType == modelType);

        return modelPermission?.IsGranted ?? true; // Default allow if no specific model restriction
    }

    /// <inheritdoc />
    public async Task<bool> UserHasObjectPermissionAsync(Guid userId, string resource, string action, Guid objectId, string objectType)
    {
        // First check base permission
        if (!await UserHasPermissionAsync(userId, resource, action)) return false;

        // Then check object-specific permission
        var objectPermission = await _context.IdentityObjectPermissions
            .Include(op => op.Permission)
            .FirstOrDefaultAsync(op => op.UserId == userId && 
                                     op.Permission.Resource == resource && 
                                     op.Permission.Action == action &&
                                     op.ObjectId == objectId &&
                                     op.ObjectType == objectType);

        return objectPermission?.IsGranted ?? true; // Default allow if no specific object restriction
    }

    // Role Permission Management
    /// <inheritdoc />
    public async Task<bool> AssignPermissionToRoleAsync(string roleName, string resource, string action, bool isGranted = true, int priority = 0)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null) return false;

        var permission = await GetPermissionAsync(resource, action);
        if (permission == null) return false;

        var rolePermission = new IdentityRolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id,
            IsGranted = isGranted,
            Priority = priority
        };

        _context.IdentityRolePermissions.Add(rolePermission);
        await _context.SaveChangesAsync();

        return true;
    }

    // Group Permission Management
    /// <inheritdoc />
    public async Task<bool> AssignPermissionToGroupAsync(Guid groupId, string resource, string action, bool isGranted = true, int priority = 0)
    {
        var permission = await GetPermissionAsync(resource, action);
        if (permission == null) return false;

        var groupPermission = new IdentityGroupPermission
        {
            GroupId = groupId,
            PermissionId = permission.Id,
            IsGranted = isGranted,
            Priority = priority
        };

        _context.IdentityGroupPermissions.Add(groupPermission);
        await _context.SaveChangesAsync();

        return true;
    }

    // User Permission Management
    /// <inheritdoc />
    public async Task<bool> AssignPermissionToUserAsync(Guid userId, string resource, string action, bool isGranted = true, int priority = 0)
    {
        var permission = await GetPermissionAsync(resource, action);
        if (permission == null) return false;

        var userPermission = new IdentityUserPermission
        {
            UserId = userId,
            PermissionId = permission.Id,
            IsGranted = isGranted,
            Priority = priority
        };

        _context.IdentityUserPermissions.Add(userPermission);
        await _context.SaveChangesAsync();

        return true;
    }

    // Helper methods
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
}