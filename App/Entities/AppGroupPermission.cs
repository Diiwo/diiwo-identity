using System.ComponentModel.DataAnnotations;

namespace Diiwo.Identity.App.Entities;

/// <summary>
///  Level 2: Group-based permissions (Priority 50)
/// </summary>
public class AppGroupPermission
{
    public AppGroupPermission()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Priority = 50;
    }

    [Key]
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public Guid PermissionId { get; set; }

    public bool IsGranted { get; set; } = true;

    public int Priority { get; set; } = 50;

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public virtual AppGroup Group { get; set; } = null!;
    public virtual AppPermission Permission { get; set; } = null!;
}