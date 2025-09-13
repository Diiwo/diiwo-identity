namespace Diiwo.Identity.Shared.Abstractions.Services;

/// <summary>
/// Identity permission service interface for AspNet architecture
/// Provides methods for checking and managing permissions in AspNet Identity system
/// </summary>
public interface IIdentityPermissionService
{
    /// <summary>
    /// Check if user has specific permission by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permissionName">Permission name</param>
    /// <returns>True if user has permission</returns>
    Task<bool> UserHasPermissionAsync(Guid userId, string permissionName);

    /// <summary>
    /// Check if user has specific permission by email
    /// </summary>
    /// <param name="userEmail">User email</param>
    /// <param name="permissionName">Permission name</param>
    /// <returns>True if user has permission</returns>
    Task<bool> UserHasPermissionAsync(string userEmail, string permissionName);

    /// <summary>
    /// Check if role has specific permission by role ID
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="permissionName">Permission name</param>
    /// <returns>True if role has permission</returns>
    Task<bool> RoleHasPermissionAsync(Guid roleId, string permissionName);

    /// <summary>
    /// Check if role has specific permission by role name
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <param name="permissionName">Permission name</param>
    /// <returns>True if role has permission</returns>
    Task<bool> RoleHasPermissionAsync(string roleName, string permissionName);

    /// <summary>
    /// Check if user has model-level permission
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permissionName">Permission name</param>
    /// <param name="modelType">Model type</param>
    /// <returns>True if user has permission</returns>
    Task<bool> UserHasModelPermissionAsync(Guid userId, string permissionName, string modelType);

    /// <summary>
    /// Check if user has object-level permission
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permissionName">Permission name</param>
    /// <param name="objectId">Object ID</param>
    /// <param name="objectType">Object type</param>
    /// <returns>True if user has permission</returns>
    Task<bool> UserHasObjectPermissionAsync(Guid userId, string permissionName, Guid objectId, string objectType);

    /// <summary>
    /// Check multiple permissions for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permissionNames">Permission names to check</param>
    /// <returns>Dictionary with permission names and results</returns>
    Task<Dictionary<string, bool>> UserHasPermissionsAsync(Guid userId, params string[] permissionNames);

    /// <summary>
    /// Grant permission to user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permissionName">Permission name</param>
    /// <param name="expiresAt">Optional expiration date</param>
    /// <param name="priority">Permission priority</param>
    /// <param name="grantedBy">ID of user granting permission</param>
    /// <returns>True if successfully granted</returns>
    Task<bool> GrantUserPermissionAsync(Guid userId, string permissionName, DateTime? expiresAt = null, int priority = 100, Guid? grantedBy = null);

    /// <summary>
    /// Revoke permission from user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permissionName">Permission name</param>
    /// <param name="revokedBy">ID of user revoking permission</param>
    /// <returns>True if successfully revoked</returns>
    Task<bool> RevokeUserPermissionAsync(Guid userId, string permissionName, Guid? revokedBy = null);
}