using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
/// ASPNET ARCHITECTURE - Enterprise role entity that extends IdentityRole
/// Combines ASP.NET Core Identity role functionality with enterprise audit features
/// Part of the 5-level permission system (Level 1 - highest priority)
/// </summary>
public class IdentityRole : IdentityRole<Guid>
{
    public IdentityRole()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public IdentityRole(string roleName) : base(roleName)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Optional foreign key to corresponding App architecture role for migration scenarios
    /// </summary>
    public Guid? AppRoleId { get; set; }

    /// <summary>
    /// When the role was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the role was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Who created this role
    /// </summary>
    public Guid? CreatedBy { get; set; }
    
    /// <summary>
    /// Who last updated this role
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Optional description explaining the role's purpose and responsibilities
    /// </summary>
    [StringLength(255)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether the role is currently active and can be assigned to users
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Permission assignments for this role (Level 1 in permission hierarchy - highest priority)
    /// </summary>
    public virtual ICollection<IdentityRolePermission> IdentityRolePermissions { get; set; } = new List<IdentityRolePermission>();
}