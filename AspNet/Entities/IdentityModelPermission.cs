using System.ComponentModel.DataAnnotations;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
/// ASPNET ARCHITECTURE - Level 4: Model-level permissions (Priority 150)
/// Enterprise version of model-specific permission assignments
/// Fourth priority in the 5-level permission hierarchy
/// </summary>
public class IdentityModelPermission
{
    public IdentityModelPermission()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Priority = 150;
    }

    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid PermissionId { get; set; }

    [Required]
    [StringLength(100)]
    public required string ModelType { get; set; }

    public bool IsGranted { get; set; } = true;

    public int Priority { get; set; } = 150;

    // Enterprise audit properties
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public virtual IdentityUser User { get; set; } = null!;
    public virtual IdentityPermission Permission { get; set; } = null!;
}