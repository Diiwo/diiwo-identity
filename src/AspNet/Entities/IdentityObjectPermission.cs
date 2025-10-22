using System.ComponentModel.DataAnnotations;
using Diiwo.Core.Domain.Interfaces;
using Diiwo.Core.Domain.Enums;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
/// ASPNET ARCHITECTURE - Level 5: Object-level permissions (Priority 200 - Lowest)
/// Enterprise version of object-specific permission assignments
/// Lowest priority in the 5-level permission hierarchy
/// </summary>
public class IdentityObjectPermission : IDomainEntity
{
    public IdentityObjectPermission()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Priority = 200;
        State = EntityState.Active;
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

    // IUserTracked implementation - allows automatic audit via AuditInterceptor
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public EntityState State { get; set; } = EntityState.Active;

    // Navigation properties
    public virtual IdentityUser User { get; set; } = null!;
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