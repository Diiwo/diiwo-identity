using System.ComponentModel.DataAnnotations;
using Diiwo.Core.Domain.Interfaces;
using Diiwo.Core.Domain.Enums;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
/// ASPNET ARCHITECTURE - Level 1: Role-based permissions (Priority 0 - Highest)
/// Enterprise version of role-based permission assignments
/// Highest priority in the 5-level permission hierarchy
/// </summary>
public class IdentityRolePermission : IDomainEntity
{
    public IdentityRolePermission()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Priority = 0; // Highest priority
        State = EntityState.Active;
    }

    /// <summary>
    /// Unique identifier for the role permission assignment
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the role that this permission is assigned to
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Foreign key to the permission being assigned
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// Whether the permission is granted (true) or explicitly denied (false)
    /// </summary>
    public bool IsGranted { get; set; } = true;

    /// <summary>
    /// Priority level for permission evaluation (0 = highest priority)
    /// </summary>
    public int Priority { get; set; } = 0;

    // IUserTracked implementation - allows automatic audit via AuditInterceptor
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public EntityState State { get; set; } = EntityState.Active;

    /// <summary>
    /// Navigation property to the associated role
    /// </summary>
    public virtual IdentityRole Role { get; set; } = null!;
    
    /// <summary>
    /// Navigation property to the associated permission
    /// </summary>
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