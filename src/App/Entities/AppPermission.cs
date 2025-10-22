using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Diiwo.Core.Domain.Entities;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.App.Entities;

/// <summary>
/// APP ARCHITECTURE - Permission definition entity
/// Defines what actions can be performed on what resources
/// Part of the 5-level permission system
/// </summary>
public class AppPermission : DomainEntity
{
    [Required]
    [StringLength(100)]
    public required string Resource { get; set; }

    [Required]
    [StringLength(100)]
    public required string Action { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }

    public PermissionScope Scope { get; set; } = PermissionScope.Global;

    /// <summary>
    /// Priority for permission evaluation when multiple permissions apply
    /// Lower values = higher priority (0 is highest priority)
    /// </summary>
    public int Priority { get; set; } = 0;

    // Note: IsActive, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy now come from DomainEntity

    // Navigation properties for 5-level permission system
    public virtual ICollection<AppRolePermission> RolePermissions { get; set; } = new List<AppRolePermission>();
    public virtual ICollection<AppGroupPermission> GroupPermissions { get; set; } = new List<AppGroupPermission>();
    public virtual ICollection<AppUserPermission> UserPermissions { get; set; } = new List<AppUserPermission>();
    public virtual ICollection<AppModelPermission> ModelPermissions { get; set; } = new List<AppModelPermission>();
    public virtual ICollection<AppObjectPermission> ObjectPermissions { get; set; } = new List<AppObjectPermission>();

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
}