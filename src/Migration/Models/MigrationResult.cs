namespace Diiwo.Identity.Migration.Models;

/// <summary>
/// Result of migration operation from App to AspNet architecture
/// </summary>
public class MigrationResult
{
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();

    // Migration statistics
    public int UsersMigrated { get; set; }
    public int RolesMigrated { get; set; }
    public int PermissionsMigrated { get; set; }
    public int GroupsMigrated { get; set; }
    public int UserSessionsMigrated { get; set; }
    public int LoginHistoryEntriesMigrated { get; set; }

    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;

    public string Summary => IsSuccessful 
        ? $"Migration completed successfully in {Duration?.TotalSeconds:F1}s. Migrated {UsersMigrated} users, {RolesMigrated} roles, {GroupsMigrated} groups, {PermissionsMigrated} permissions."
        : $"Migration failed: {ErrorMessage}";
}

/// <summary>
/// Result of migration validation
/// </summary>
public class MigrationValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();

    // Record counts for comparison
    public int AppUsers { get; set; }
    public int AspNetUsers { get; set; }
    public int AppRoles { get; set; }
    public int AspNetRoles { get; set; }
    public int AppPermissions { get; set; }
    public int AspNetPermissions { get; set; }
    public int AppGroups { get; set; }
    public int AspNetGroups { get; set; }

    public string Summary => IsValid 
        ? "Migration validation passed - all record counts match"
        : $"Migration validation failed: {string.Join(", ", ValidationErrors)}";
}