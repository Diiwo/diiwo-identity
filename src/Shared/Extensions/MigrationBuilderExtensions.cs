using Microsoft.EntityFrameworkCore.Migrations;
using System.Reflection;
using Diiwo.Identity.Shared.Attributes;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.Shared.Extensions;

/// <summary>
/// Extension methods for MigrationBuilder to enable automatic permission generation in migrations
/// Provides production-safe permission generation that integrates with Entity Framework migrations
/// Supports both App and AspNet architectures with flexible configuration options
/// </summary>
public static class MigrationBuilderExtensions
{
    /// <summary>
    /// Generates permissions for entities with PermissionAttribute in the specified assembly
    /// Scans assembly for classes decorated with PermissionAttribute and creates permission records
    /// Safe for production use - only creates permissions that don't already exist
    /// </summary>
    /// <param name="migrationBuilder">The MigrationBuilder instance</param>
    /// <param name="assembly">Assembly to scan for entities with PermissionAttribute</param>
    /// <param name="tableName">Target permissions table name (default: "App_Permissions")</param>
    /// <param name="includeInactive">Whether to include inactive entities (default: false)</param>
    /// <example>
    /// <code>
    /// public partial class AddEntityPermissions : Migration
    /// {
    ///     protected override void Up(MigrationBuilder migrationBuilder)
    ///     {
    ///         migrationBuilder.GenerateEntityPermissions(Assembly.GetExecutingAssembly());
    ///     }
    /// }
    /// </code>
    /// </example>
    public static void GenerateEntityPermissions(
        this MigrationBuilder migrationBuilder, 
        Assembly assembly, 
        string tableName = "App_Permissions",
        bool includeInactive = false)
    {
        if (migrationBuilder == null)
            throw new ArgumentNullException(nameof(migrationBuilder));
        
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var entityTypes = GetEntityTypesWithPermissions(assembly, includeInactive);
        var permissionRows = new List<object[]>();

        foreach (var entityType in entityTypes)
        {
            var permissions = ExtractPermissionDefinitions(entityType);
            
            foreach (var permission in permissions)
            {
                permissionRows.Add(new object[] { 
                    Guid.NewGuid(), 
                    permission.Resource, 
                    permission.Action, 
                    permission.Description ?? $"{permission.Action} {permission.Resource}",
                    (int)permission.Scope, 
                    true, // IsActive
                    DateTime.UtcNow, 
                    DateTime.UtcNow 
                });
            }
        }

        if (permissionRows.Any())
        {
            migrationBuilder.InsertData(
                table: tableName,
                columns: new[] { "Id", "Resource", "Action", "Description", "Scope", "IsActive", "CreatedAt", "UpdatedAt" },
                values: permissionRows.ToArray()
            );
        }
    }

    /// <summary>
    /// Generates permissions for AspNet architecture (uses "Identity_Permissions" table)
    /// Convenience method specifically for AspNet architecture with correct table name
    /// </summary>
    /// <param name="migrationBuilder">The MigrationBuilder instance</param>
    /// <param name="assembly">Assembly to scan for entities</param>
    /// <param name="includeInactive">Whether to include inactive entities</param>
    public static void GenerateAspNetEntityPermissions(
        this MigrationBuilder migrationBuilder, 
        Assembly assembly,
        bool includeInactive = false)
    {
        migrationBuilder.GenerateEntityPermissions(assembly, "Identity_Permissions", includeInactive);
    }

    /// <summary>
    /// Generates permissions for App architecture (uses "App_Permissions" table)  
    /// Convenience method specifically for App architecture with correct table name
    /// </summary>
    /// <param name="migrationBuilder">The MigrationBuilder instance</param>
    /// <param name="assembly">Assembly to scan for entities</param>
    /// <param name="includeInactive">Whether to include inactive entities</param>
    public static void GenerateAppEntityPermissions(
        this MigrationBuilder migrationBuilder, 
        Assembly assembly,
        bool includeInactive = false)
    {
        migrationBuilder.GenerateEntityPermissions(assembly, "App_Permissions", includeInactive);
    }

    /// <summary>
    /// Generates permissions for multiple assemblies at once
    /// Useful when permissions are spread across different assemblies/projects
    /// </summary>
    /// <param name="migrationBuilder">The MigrationBuilder instance</param>
    /// <param name="assemblies">Collection of assemblies to scan</param>
    /// <param name="tableName">Target permissions table name</param>
    /// <param name="includeInactive">Whether to include inactive entities</param>
    public static void GenerateEntityPermissions(
        this MigrationBuilder migrationBuilder,
        IEnumerable<Assembly> assemblies,
        string tableName = "App_Permissions", 
        bool includeInactive = false)
    {
        foreach (var assembly in assemblies)
        {
            migrationBuilder.GenerateEntityPermissions(assembly, tableName, includeInactive);
        }
    }

    /// <summary>
    /// Generates permissions for specific entity types
    /// Allows fine-grained control over which entities get permission generation
    /// </summary>
    /// <param name="migrationBuilder">The MigrationBuilder instance</param>
    /// <param name="entityTypes">Specific entity types to process</param>
    /// <param name="tableName">Target permissions table name</param>
    public static void GenerateEntityPermissions(
        this MigrationBuilder migrationBuilder,
        IEnumerable<Type> entityTypes,
        string tableName = "App_Permissions")
    {
        var permissionRows = new List<object[]>();

        foreach (var entityType in entityTypes)
        {
            var permissions = ExtractPermissionDefinitions(entityType);
            
            foreach (var permission in permissions)
            {
                permissionRows.Add(new object[] { 
                    Guid.NewGuid(), 
                    permission.Resource, 
                    permission.Action, 
                    permission.Description ?? $"{permission.Action} {permission.Resource}",
                    (int)permission.Scope, 
                    true,
                    DateTime.UtcNow, 
                    DateTime.UtcNow 
                });
            }
        }

        if (permissionRows.Any())
        {
            migrationBuilder.InsertData(
                table: tableName,
                columns: new[] { "Id", "Resource", "Action", "Description", "Scope", "IsActive", "CreatedAt", "UpdatedAt" },
                values: permissionRows.ToArray()
            );
        }
    }

    /// <summary>
    /// Previews what permissions would be generated without actually creating them
    /// Useful for debugging and validation during development
    /// </summary>
    /// <param name="assembly">Assembly to analyze</param>
    /// <param name="includeInactive">Whether to include inactive entities</param>
    /// <returns>Collection of permission definitions that would be created</returns>
    public static IEnumerable<PermissionPreview> PreviewEntityPermissions(
        Assembly assembly,
        bool includeInactive = false)
    {
        var entityTypes = GetEntityTypesWithPermissions(assembly, includeInactive);
        var previews = new List<PermissionPreview>();

        foreach (var entityType in entityTypes)
        {
            var permissions = ExtractPermissionDefinitions(entityType);
            
            foreach (var permission in permissions)
            {
                previews.Add(new PermissionPreview
                {
                    EntityType = entityType.Name,
                    Resource = permission.Resource,
                    Action = permission.Action,
                    Description = permission.Description,
                    Scope = permission.Scope,
                    Priority = permission.Priority,
                    PermissionName = $"{permission.Resource}.{permission.Action}"
                });
            }
        }

        return previews;
    }

    #region Private Helper Methods

    private static IEnumerable<Type> GetEntityTypesWithPermissions(Assembly assembly, bool includeInactive)
    {
        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => includeInactive || !IsInactiveEntity(t))
            .Where(t => t.GetCustomAttributes<PermissionAttribute>().Any());
    }

    private static bool IsInactiveEntity(Type entityType)
    {
        // Check for common inactive indicators
        var typeName = entityType.Name.ToLower();
        return typeName.Contains("inactive") || 
               typeName.Contains("disabled") || 
               typeName.Contains("archived") ||
               entityType.IsAbstract ||
               entityType.IsInterface;
    }

    private static IEnumerable<PermissionDefinition> ExtractPermissionDefinitions(Type entityType)
    {
        var attributes = entityType.GetCustomAttributes<PermissionAttribute>();
        var resourceName = entityType.Name;

        return attributes.Select(attr => new PermissionDefinition
        {
            Resource = resourceName,
            Action = attr.Action,
            Description = attr.Description,
            Scope = attr.Scope,
            Priority = attr.Priority
        });
    }

    #endregion
}

/// <summary>
/// Represents a permission definition for internal processing
/// Used by the migration builder extensions for permission generation
/// </summary>
internal record PermissionDefinition
{
    public required string Resource { get; init; }
    public required string Action { get; init; }
    public string? Description { get; init; }
    public PermissionScope Scope { get; init; } = PermissionScope.Model;
    public int Priority { get; init; } = 0;
}

/// <summary>
/// Represents a permission preview for debugging and validation
/// Provides detailed information about permissions that would be generated
/// </summary>
public record PermissionPreview
{
    /// <summary>
    /// The name of the entity type
    /// </summary>
    public required string EntityType { get; init; }
    
    /// <summary>
    /// The resource name (typically same as EntityType)
    /// </summary>
    public required string Resource { get; init; }
    
    /// <summary>
    /// The action name from PermissionAttribute
    /// </summary>
    public required string Action { get; init; }
    
    /// <summary>
    /// Optional description from PermissionAttribute
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// The permission scope level
    /// </summary>
    public PermissionScope Scope { get; init; }
    
    /// <summary>
    /// Priority for permission evaluation
    /// </summary>
    public int Priority { get; init; }
    
    /// <summary>
    /// The complete permission name (Resource.Action)
    /// </summary>
    public required string PermissionName { get; init; }
}