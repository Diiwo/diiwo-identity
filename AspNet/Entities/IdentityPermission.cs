using System.ComponentModel.DataAnnotations;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
///  ASPNET ARCHITECTURE - Enterprise permission definition entity
/// Defines what actions can be performed on what resources
/// Part of the 5-level permission system with enterprise features
/// </summary>
public class IdentityPermission
{
    public IdentityPermission()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unique identifier for the permission
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Resource name that the permission applies to (e.g., "User", "Document", "Report")
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string Resource { get; set; }

    /// <summary>
    /// Action that can be performed on the resource (e.g., "Read", "Write", "Delete")
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string Action { get; set; }

    /// <summary>
    /// Optional description explaining what this permission allows
    /// </summary>
    [StringLength(255)]
    public string? Description { get; set; }

    /// <summary>
    /// Scope of the permission (Global, Model, Object)
    /// </summary>
    public PermissionScope Scope { get; set; } = PermissionScope.Global;

    /// <summary>
    /// Whether the permission is currently active and can be granted
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional foreign key to corresponding App architecture permission for migration scenarios
    /// </summary>
    public Guid? AppPermissionId { get; set; }

    /// <summary>
    /// When the permission was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the permission was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Who created this permission
    /// </summary>
    public Guid? CreatedBy { get; set; }
    
    /// <summary>
    /// Who last updated this permission
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Role-level permission assignments (Level 1 - highest priority)
    /// </summary>
    public virtual ICollection<IdentityRolePermission> IdentityRolePermissions { get; set; } = new List<IdentityRolePermission>();
    
    /// <summary>
    /// Group-level permission assignments (Level 2)
    /// </summary>
    public virtual ICollection<IdentityGroupPermission> IdentityGroupPermissions { get; set; } = new List<IdentityGroupPermission>();
    
    /// <summary>
    /// User-level permission assignments (Level 3)
    /// </summary>
    public virtual ICollection<IdentityUserPermission> IdentityUserPermissions { get; set; } = new List<IdentityUserPermission>();
    
    /// <summary>
    /// Model-level permission assignments (Level 4)
    /// </summary>
    public virtual ICollection<IdentityModelPermission> IdentityModelPermissions { get; set; } = new List<IdentityModelPermission>();
    
    /// <summary>
    /// Object-level permission assignments (Level 5 - lowest priority)
    /// </summary>
    public virtual ICollection<IdentityObjectPermission> IdentityObjectPermissions { get; set; } = new List<IdentityObjectPermission>();

    /// <summary>
    /// Get permission name in format: Resource.Action
    /// </summary>
    public string Name => $"{Resource}.{Action}";

    /// <summary>
    /// Check if this permission matches a given name
    /// </summary>
    public bool Matches(string permissionName)
    {
        return string.Equals(Name, permissionName, StringComparison.OrdinalIgnoreCase);
    }
}