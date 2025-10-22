using System.ComponentModel.DataAnnotations;
using Diiwo.Core.Domain.Interfaces;
using Diiwo.Core.Domain.Enums;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
/// ASPNET ARCHITECTURE - Level 2: Group-based permissions (Priority 50)
/// Enterprise version of group-based permission assignments
/// Second highest priority in the 5-level permission hierarchy
/// </summary>
public class IdentityGroupPermission : IDomainEntity
{
    public IdentityGroupPermission()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Priority = 50;
        State = EntityState.Active;
    }

    [Key]
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public Guid PermissionId { get; set; }

    public bool IsGranted { get; set; } = true;

    public int Priority { get; set; } = 50;

    // IUserTracked implementation - allows automatic audit via AuditInterceptor
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public EntityState State { get; set; } = EntityState.Active;

    // Navigation properties
    public virtual IdentityGroup Group { get; set; } = null!;
    public virtual IdentityPermission Permission { get; set; } = null!;

    /// <summary>
    /// IDomainEntity implementation - Soft delete the entity
    /// </summary>
    public void SoftDelete()
    {
        State = EntityState.Terminated;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// IDomainEntity implementation - Restore a soft-deleted entity
    /// </summary>
    public void Restore()
    {
        State = EntityState.Active;
        UpdatedAt = DateTime.UtcNow;
    }
}