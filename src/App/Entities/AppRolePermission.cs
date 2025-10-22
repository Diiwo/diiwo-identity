using System.ComponentModel.DataAnnotations;
using Diiwo.Core.Domain.Entities;

namespace Diiwo.Identity.App.Entities;

/// <summary>
/// Level 1: Role-based permissions (Priority 0 - Highest)
/// Assigns permissions directly to roles in the 5-level permission system
/// </summary>
public class AppRolePermission : DomainEntity
{
    public AppRolePermission()
    {
        Priority = 0; // Highest priority
    }

    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }

    public bool IsGranted { get; set; } = true;

    public int Priority { get; set; } = 0;

    // Note: Audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) and IsActive come from DomainEntity

    // Navigation properties
    public virtual AppRole Role { get; set; } = null!;
    public virtual AppPermission Permission { get; set; } = null!;
}