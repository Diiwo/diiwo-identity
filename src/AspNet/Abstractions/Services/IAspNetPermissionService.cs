using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.AspNet.Abstractions.Services;

/// <summary>
/// AspNet permission service interface for AspNet architecture
/// Enterprise permission management with full ASP.NET Core Identity integration
/// Provides comprehensive permission operations including creation, assignment, and checking
/// </summary>
public interface IAspNetPermissionService
{
    /// <summary>
    /// Creates a new permission in the system
    /// </summary>
    /// <param name="resource">The resource the permission applies to</param>
    /// <param name="action">The action that can be performed</param>
    /// <param name="description">Optional description of the permission</param>
    /// <param name="scope">The scope of the permission</param>
    /// <returns>The created permission</returns>
    Task<IdentityPermission> CreatePermissionAsync(string resource, string action, string? description = null, PermissionScope scope = PermissionScope.Global);

    /// <summary>
    /// Gets all active permissions in the system
    /// </summary>
    /// <returns>List of active permissions ordered by resource and action</returns>
    Task<List<IdentityPermission>> GetAllPermissionsAsync();

    /// <summary>
    /// Gets a specific permission by resource and action
    /// </summary>
    /// <param name="resource">The resource name</param>
    /// <param name="action">The action name</param>
    /// <returns>The permission if found, null otherwise</returns>
    Task<IdentityPermission?> GetPermissionAsync(string resource, string action);

    /// <summary>
    /// Checks if a user has a specific permission through role, group, or direct assignment
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="resource">The resource to check permission for</param>
    /// <param name="action">The action to check permission for</param>
    /// <returns>True if the user has the permission, false otherwise</returns>
    Task<bool> UserHasPermissionAsync(Guid userId, string resource, string action);

    /// <summary>
    /// Checks if a user has model-level permission for a specific model type
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="resource">The resource to check permission for</param>
    /// <param name="action">The action to check permission for</param>
    /// <param name="modelType">The type of model</param>
    /// <returns>True if the user has the model permission, false otherwise</returns>
    Task<bool> UserHasModelPermissionAsync(Guid userId, string resource, string action, string modelType);

    /// <summary>
    /// Checks if a user has object-level permission for a specific object instance
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="resource">The resource to check permission for</param>
    /// <param name="action">The action to check permission for</param>
    /// <param name="objectId">The unique identifier of the object</param>
    /// <param name="objectType">The type of the object</param>
    /// <returns>True if the user has the object permission, false otherwise</returns>
    Task<bool> UserHasObjectPermissionAsync(Guid userId, string resource, string action, Guid objectId, string objectType);

    /// <summary>
    /// Assigns a permission to a role (Level 1 - highest priority)
    /// </summary>
    /// <param name="roleName">The name of the role</param>
    /// <param name="resource">The resource name</param>
    /// <param name="action">The action name</param>
    /// <param name="isGranted">Whether the permission is granted or denied</param>
    /// <param name="priority">The priority of this permission assignment</param>
    /// <returns>True if the assignment was successful, false otherwise</returns>
    Task<bool> AssignPermissionToRoleAsync(string roleName, string resource, string action, bool isGranted = true, int priority = 0);

    /// <summary>
    /// Assigns a permission to a group (Level 2)
    /// </summary>
    /// <param name="groupId">The unique identifier of the group</param>
    /// <param name="resource">The resource name</param>
    /// <param name="action">The action name</param>
    /// <param name="isGranted">Whether the permission is granted or denied</param>
    /// <param name="priority">The priority of this permission assignment</param>
    /// <returns>True if the assignment was successful, false otherwise</returns>
    Task<bool> AssignPermissionToGroupAsync(Guid groupId, string resource, string action, bool isGranted = true, int priority = 0);

    /// <summary>
    /// Assigns a permission directly to a user (Level 3)
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="resource">The resource name</param>
    /// <param name="action">The action name</param>
    /// <param name="isGranted">Whether the permission is granted or denied</param>
    /// <param name="priority">The priority of this permission assignment</param>
    /// <returns>True if the assignment was successful, false otherwise</returns>
    Task<bool> AssignPermissionToUserAsync(Guid userId, string resource, string action, bool isGranted = true, int priority = 0);
}