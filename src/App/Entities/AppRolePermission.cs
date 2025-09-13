using System.ComponentModel.DataAnnotations;

namespace Diiwo.Identity.App.Entities;

/// <summary>
/// Level 1: Role-based permissions (Priority 0 - Highest)
/// Assigns permissions directly to roles in the 5-level permission system
/// </summary>
public class AppRolePermission
{
    public AppRolePermission()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Priority = 0; // Highest priority
    }

    [Key]
    public Guid Id { get; set; }

    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }

    public bool IsGranted { get; set; } = true;

    public int Priority { get; set; } = 0;

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public virtual AppRole Role { get; set; } = null!;
    public virtual AppPermission Permission { get; set; } = null!;
}