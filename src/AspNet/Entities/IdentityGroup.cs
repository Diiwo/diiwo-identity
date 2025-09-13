using System.ComponentModel.DataAnnotations;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
/// ASPNET ARCHITECTURE - Enterprise user group entity
/// Allows grouping users for easier permission management
/// Part of the 5-level permission system (Level 2) with enterprise features
/// </summary>
public class IdentityGroup
{
    public IdentityGroup()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unique identifier for the group
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the group (e.g., "Administrators", "Editors", "Viewers")
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// Optional description explaining the group's purpose
    /// </summary>
    [StringLength(255)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether the group is currently active and can be assigned to users
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional foreign key to corresponding App architecture group for migration scenarios
    /// </summary>
    public Guid? AppGroupId { get; set; }

    /// <summary>
    /// When the group was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the group was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Who created this group
    /// </summary>
    public Guid? CreatedBy { get; set; }
    
    /// <summary>
    /// Who last updated this group
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Users that belong to this group
    /// </summary>
    public virtual ICollection<IdentityUser> Users { get; set; } = new List<IdentityUser>();
    
    /// <summary>
    /// Permission assignments for this group (Level 2 in permission hierarchy)
    /// </summary>
    public virtual ICollection<IdentityGroupPermission> GroupPermissions { get; set; } = new List<IdentityGroupPermission>();

    /// <summary>
    /// Get all active users in this group
    /// </summary>
    public IEnumerable<IdentityUser> GetActiveUsers() => Users.Where(u => u.CanLogin);

    /// <summary>
    /// Get all permissions granted to this group
    /// </summary>
    public IEnumerable<IdentityPermission> GetPermissions() => GroupPermissions
        .Where(gp => gp.IsGranted)
        .Select(gp => gp.Permission);
}