using System.ComponentModel.DataAnnotations;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
/// ASPNET ARCHITECTURE - Level 5: Object-level permissions (Priority 200 - Lowest)
/// Enterprise version of object-specific permission assignments
/// Lowest priority in the 5-level permission hierarchy
/// </summary>
public class IdentityObjectPermission
{
    public IdentityObjectPermission()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Priority = 200;
    }

    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid PermissionId { get; set; }

    public Guid ObjectId { get; set; }

    [Required]
    [StringLength(100)]
    public required string ObjectType { get; set; }

    public bool IsGranted { get; set; } = true;

    public int Priority { get; set; } = 200;

    // Enterprise audit properties
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public virtual IdentityUser User { get; set; } = null!;
    public virtual IdentityPermission Permission { get; set; } = null!;
}