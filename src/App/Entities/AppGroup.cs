using System.ComponentModel.DataAnnotations;
using Diiwo.Core.Domain.Entities;

namespace Diiwo.Identity.App.Entities;

/// <summary>
/// APP ARCHITECTURE - User group entity
/// Allows grouping users for easier permission management
/// Part of the 5-level permission system (Level 2)
/// </summary>
public class AppGroup : DomainEntity
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public virtual ICollection<AppGroupPermission> GroupPermissions { get; set; } = new List<AppGroupPermission>();

    /// <summary>
    /// Get all permissions granted to this group
    /// This is a computed property, not a navigation property
    /// </summary>
    public IEnumerable<AppPermission> GetGrantedPermissions() => GroupPermissions
        .Where(gp => gp.IsGranted)
        .Select(gp => gp.Permission);
}