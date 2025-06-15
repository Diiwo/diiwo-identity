using System.ComponentModel.DataAnnotations;

namespace Diiwo.Identity.App.Entities;

/// <summary>
///  Level 3: User-specific permissions (Priority 100)
/// </summary>
public class AppUserPermission
{
    public AppUserPermission()
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

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public virtual AppUser User { get; set; } = null!;
    public virtual AppPermission Permission { get; set; } = null!;

    /// <summary>
    /// Check if permission has expired
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt <= DateTime.UtcNow;
}