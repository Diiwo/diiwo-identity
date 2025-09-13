namespace Diiwo.Identity.Shared.Abstractions.Services;

/// <summary>
/// App permission service interface for App architecture
/// Provides methods for checking and managing permissions in simple App system
/// Implements 5-level permission hierarchy: Role → Group → User → Model → Object
/// Priority: Role (0) > Group (50) > User (100) > Model (150) > Object (200)
/// </summary>
public interface IAppPermissionService
{
    /// <summary>
    /// Check if user has specific permission by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="resource">Resource name (e.g., "User", "Document")</param>
    /// <param name="action">Action name (e.g., "Read", "Write", "Delete")</param>
    /// <returns>True if user has permission</returns>
    Task<bool> UserHasPermissionAsync(Guid userId, string resource, string action);

    /// <summary>
    /// Check if user has model-level permission
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="resource">Resource name</param>
    /// <param name="action">Action name</param>
    /// <param name="modelType">Model type</param>
    /// <returns>True if user has permission</returns>
    Task<bool> UserHasModelPermissionAsync(Guid userId, string resource, string action, string modelType);

    /// <summary>
    /// Check if user has object-level permission
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="resource">Resource name</param>
    /// <param name="action">Action name</param>
    /// <param name="objectId">Object ID</param>
    /// <param name="objectType">Object type</param>
    /// <returns>True if user has permission</returns>
    Task<bool> UserHasObjectPermissionAsync(Guid userId, string resource, string action, Guid objectId, string objectType);

    /// <summary>
    /// Check multiple permissions for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permissions">Array of permission strings in format "Resource.Action"</param>
    /// <returns>Dictionary with permission strings and results</returns>
    Task<Dictionary<string, bool>> UserHasPermissionsAsync(Guid userId, params string[] permissions);

    /// <summary>
    /// Grant permission to user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="resource">Resource name</param>
    /// <param name="action">Action name</param>
    /// <param name="expiresAt">Optional expiration date</param>
    /// <param name="priority">Permission priority (default 100)</param>
    /// <param name="grantedBy">ID of user granting permission</param>
    /// <returns>True if successfully granted</returns>
    Task<bool> GrantUserPermissionAsync(Guid userId, string resource, string action, DateTime? expiresAt = null, int priority = 100, Guid? grantedBy = null);

    /// <summary>
    /// Revoke permission from user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="resource">Resource name</param>
    /// <param name="action">Action name</param>
    /// <param name="revokedBy">ID of user revoking permission</param>
    /// <returns>True if successfully revoked</returns>
    Task<bool> RevokeUserPermissionAsync(Guid userId, string resource, string action, Guid? revokedBy = null);

    /// <summary>
    /// Get all permissions for user (direct, role, and group based)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of permission strings in format "Resource.Action"</returns>
    Task<List<string>> GetUserPermissionsAsync(Guid userId);

    /// <summary>
    /// Check if role has specific permission
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="resource">Resource name</param>
    /// <param name="action">Action name</param>
    /// <returns>True if role has permission</returns>
    Task<bool> RoleHasPermissionAsync(Guid roleId, string resource, string action);

    /// <summary>
    /// Check if group has specific permission
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="resource">Resource name</param>
    /// <param name="action">Action name</param>
    /// <returns>True if group has permission</returns>
    Task<bool> GroupHasPermissionAsync(Guid groupId, string resource, string action);
}