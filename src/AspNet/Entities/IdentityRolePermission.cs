using System.ComponentModel.DataAnnotations;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
/// ASPNET ARCHITECTURE - Level 1: Role-based permissions (Priority 0 - Highest)
/// Enterprise version of role-based permission assignments
/// Highest priority in the 5-level permission hierarchy
/// </summary>
public class IdentityRolePermission
{
    public IdentityRolePermission()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Priority = 0; // Highest priority
    }

    /// <summary>
    /// Unique identifier for the role permission assignment
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the role that this permission is assigned to
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Foreign key to the permission being assigned
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// Whether the permission is granted (true) or explicitly denied (false)
    /// </summary>
    public bool IsGranted { get; set; } = true;

    /// <summary>
    /// Priority level for permission evaluation (0 = highest priority)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// When the permission assignment was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the permission assignment was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Who created this permission assignment
    /// </summary>
    public Guid? CreatedBy { get; set; }
    
    /// <summary>
    /// Who last updated this permission assignment
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Navigation property to the associated role
    /// </summary>
    public virtual IdentityRole Role { get; set; } = null!;
    
    /// <summary>
    /// Navigation property to the associated permission
    /// </summary>
    public virtual IdentityPermission Permission { get; set; } = null!;
}