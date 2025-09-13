using System.ComponentModel.DataAnnotations;

namespace Diiwo.Identity.AspNet.Entities;

/// <summary>
/// ASPNET ARCHITECTURE - Level 3: User-specific permissions (Priority 100)
/// Enterprise version of user-specific permission assignments
/// Middle priority in the 5-level permission hierarchy
/// </summary>
public class IdentityUserPermission
{
    public IdentityUserPermission()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Priority = 100;
    }

    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid PermissionId { get; set; }

    public bool IsGranted { get; set; } = true;

    public int Priority { get; set; } = 100;

    public DateTime? ExpiresAt { get; set; }

    // Enterprise audit properties
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public virtual IdentityUser User { get; set; } = null!;
    public virtual IdentityPermission Permission { get; set; } = null!;

    /// <summary>
    /// Check if permission has expired
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt <= DateTime.UtcNow;
}