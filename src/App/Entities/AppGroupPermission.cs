using System.ComponentModel.DataAnnotations;
using Diiwo.Core.Domain.Entities;

namespace Diiwo.Identity.App.Entities;

/// <summary>
///  Level 2: Group-based permissions (Priority 50)
/// </summary>
public class AppGroupPermission : DomainEntity
{
    public AppGroupPermission()
    {
        Priority = 50;
    }

    public Guid GroupId { get; set; }

    public Guid PermissionId { get; set; }

    public bool IsGranted { get; set; } = true;

    public int Priority { get; set; } = 50;

    // Note: Audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) and IsActive come from DomainEntity

    // Navigation properties
    public virtual AppGroup Group { get; set; } = null!;
    public virtual AppPermission Permission { get; set; } = null!;
}