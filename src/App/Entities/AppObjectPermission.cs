using System.ComponentModel.DataAnnotations;
using Diiwo.Core.Domain.Entities;

namespace Diiwo.Identity.App.Entities;

/// <summary>
///  Level 5: Object-level permissions (Priority 200 - Lowest)
/// </summary>
public class AppObjectPermission : DomainEntity
{
    public AppObjectPermission()
    {
        Priority = 200;
    }

    public Guid UserId { get; set; }

    public Guid PermissionId { get; set; }

    public Guid ObjectId { get; set; }

    [Required]
    [StringLength(100)]
    public required string ObjectType { get; set; }

    public bool IsGranted { get; set; } = true;

    public int Priority { get; set; } = 200;

    // Note: Audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) and IsActive come from DomainEntity

    // Navigation properties
    public virtual AppUser User { get; set; } = null!;
    public virtual AppPermission Permission { get; set; } = null!;
}