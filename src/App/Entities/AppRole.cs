using System.ComponentModel.DataAnnotations;
using Diiwo.Core.Domain.Entities;

namespace Diiwo.Identity.App.Entities;

/// <summary>
/// APP ARCHITECTURE - Simple role entity
/// Basic role definition for simple projects
/// </summary>
public class AppRole : UserTrackedEntity
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<AppRolePermission> RolePermissions { get; set; } = new List<AppRolePermission>();
}