using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.Shared.Attributes;

/// <summary>
/// Attribute to automatically generate permissions for entities
/// Place this attribute on entity classes to define what permissions should be created
/// Supports multiple permissions per entity with custom descriptions and scopes
/// </summary>
/// <example>
/// <code>
/// [Permission("Read", "View appointments")]
/// [Permission("Create", "Schedule new appointments")]
/// [Permission("Update", "Modify existing appointments")]
/// [Permission("Cancel", "Cancel appointments")]
/// public class Appointment
/// {
///     // Entity properties...
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PermissionAttribute : Attribute
{
    /// <summary>
    /// The action name for the permission (e.g., "Read", "Create", "Cancel")
    /// Will be combined with entity name to create permission key (e.g., "Appointment.Read")
    /// </summary>
    public string Action { get; }
    
    /// <summary>
    /// Optional human-readable description of what this permission allows
    /// Used for documentation and UI display purposes
    /// </summary>
    public string? Description { get; }
    
    /// <summary>
    /// The scope level for this permission in the 5-level system
    /// Determines how specific or broad the permission application is
    /// </summary>
    public PermissionScope Scope { get; }
    
    /// <summary>
    /// Optional priority for permission evaluation
    /// Higher values take precedence in permission resolution
    /// Default is 0 (standard priority)
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Creates a permission attribute for automatic permission generation
    /// </summary>
    /// <param name="action">The action name (e.g., "Read", "Create", "Delete")</param>
    /// <param name="description">Optional description for the permission</param>
    /// <param name="scope">The permission scope level (default: Model)</param>
    /// <param name="priority">Priority for permission evaluation (default: 0)</param>
    public PermissionAttribute(string action, string? description = null, PermissionScope scope = PermissionScope.Model, int priority = 0)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Description = description;
        Scope = scope;
        Priority = priority;
    }
}