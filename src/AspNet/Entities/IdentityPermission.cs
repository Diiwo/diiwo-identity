using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Diiwo.Core.Domain.Interfaces;
using Diiwo.Core.Domain.Enums;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
///  ASPNET ARCHITECTURE - Enterprise permission definition entity
/// Defines what actions can be performed on what resources
/// Part of the 5-level permission system with enterprise features
/// </summary>
public class IdentityPermission : IDomainEntity
{
    public IdentityPermission()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        State = EntityState.Active;
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
    /// Priority for permission evaluation when multiple permissions apply
    /// Lower values = higher priority (0 is highest priority)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Current state of the permission (Active, Inactive, Terminated)
    /// </summary>
    public EntityState State { get; set; } = EntityState.Active;

    /// <summary>
    /// Whether the permission is currently active and can be granted
    /// </summary>
    [NotMapped]
    public bool IsActive => State == EntityState.Active;

    /// <summary>
    /// Optional foreign key to corresponding App architecture permission for migration scenarios
    /// </summary>
    public Guid? AppPermissionId { get; set; }

    // IUserTracked implementation - allows automatic audit via AuditInterceptor
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
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
    [NotMapped]
    public string Name => $"{Resource}.{Action}";

    /// <summary>
    /// Check if this permission matches a given name
    /// </summary>
    public bool Matches(string permissionName)
    {
        return string.Equals(Name, permissionName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// IDomainEntity implementation - Soft delete the entity
    /// </summary>
    public void SoftDelete()
    {
        State = EntityState.Terminated;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// IDomainEntity implementation - Restore a soft-deleted entity
    /// </summary>
    public void Restore()
    {
        State = EntityState.Active;
        UpdatedAt = DateTime.UtcNow;
    }
}