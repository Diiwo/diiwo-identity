using System.ComponentModel.DataAnnotations;
using Diiwo.Core.Domain.Entities;

namespace Diiwo.Identity.App.Entities;

/// <summary>
///  Level 4: Model-level permissions (Priority 150)
/// </summary>
public class AppModelPermission : DomainEntity
{
    public AppModelPermission()
    {
        Priority = 150;
    }

    public Guid UserId { get; set; }

    public Guid PermissionId { get; set; }

    [Required]
    [StringLength(100)]
    public required string ModelType { get; set; }

    public bool IsGranted { get; set; } = true;

    public int Priority { get; set; } = 150;

    // Note: Audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) and IsActive come from DomainEntity

    // Navigation properties
    public virtual AppUser User { get; set; } = null!;
    public virtual AppPermission Permission { get; set; } = null!;
}