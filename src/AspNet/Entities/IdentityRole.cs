using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Diiwo.Core.Domain.Interfaces;
using Diiwo.Core.Domain.Enums;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
/// ASPNET ARCHITECTURE - Enterprise role entity that extends IdentityRole
/// Combines ASP.NET Core Identity role functionality with enterprise audit features
/// Part of the 5-level permission system (Level 1 - highest priority)
/// </summary>
public class IdentityRole : IdentityRole<Guid>, IDomainEntity
{
    public IdentityRole()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        State = EntityState.Active;
    }

    public IdentityRole(string roleName) : base(roleName)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        State = EntityState.Active;
    }

    /// <summary>
    /// Optional foreign key to corresponding App architecture role for migration scenarios
    /// </summary>
    public Guid? AppRoleId { get; set; }

    // IUserTracked implementation - allows automatic audit via AuditInterceptor
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public EntityState State { get; set; } = EntityState.Active;

    /// <summary>
    /// Optional description explaining the role's purpose and responsibilities
    /// </summary>
    [StringLength(255)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether the role is currently active and can be assigned to users
    /// </summary>
    [NotMapped]
    public bool IsActive => State == EntityState.Active;

    /// <summary>
    /// Permission assignments for this role (Level 1 in permission hierarchy - highest priority)
    /// </summary>
    public virtual ICollection<IdentityRolePermission> IdentityRolePermissions { get; set; } = new List<IdentityRolePermission>();

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