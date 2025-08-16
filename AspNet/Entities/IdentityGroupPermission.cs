using System.ComponentModel.DataAnnotations;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
/// ASPNET ARCHITECTURE - Level 2: Group-based permissions (Priority 50)
/// Enterprise version of group-based permission assignments
/// Second highest priority in the 5-level permission hierarchy
/// </summary>
public class IdentityGroupPermission
{
    public IdentityGroupPermission()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Priority = 50;
    }

    [Key]
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public Guid PermissionId { get; set; }

    public bool IsGranted { get; set; } = true;

    public int Priority { get; set; } = 50;

    // Enterprise audit properties
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public virtual IdentityGroup Group { get; set; } = null!;
    public virtual IdentityPermission Permission { get; set; } = null!;
}