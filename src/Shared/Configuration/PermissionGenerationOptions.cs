namespace Diiwo.Identity.Shared.Configuration;

/// <summary>
/// Simple configuration for automatic permission generation
/// All settings are optional with intelligent defaults based on environment
/// </summary>
public class PermissionGenerationOptions
{
    /// <summary>
    /// Configuration section name for appsettings.json
    /// </summary>
    public const string SectionName = "PermissionGeneration";

    /// <summary>
    /// Whether to automatically generate permissions
    /// Default: true in Development, false in Production
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Assembly names to scan for entities with PermissionAttribute
    /// Default: scans current executing assembly
    /// </summary>
    public string[]? ScanAssemblies { get; set; }

    /// <summary>
    /// Enable detailed logging during permission generation
    /// Default: false (only basic information logged)
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// Skip generation if any permissions already exist in database
    /// Default: false (only skips individual duplicate permissions)
    /// </summary>
    public bool SkipIfPermissionsExist { get; set; } = false;

    /// <summary>
    /// Custom table name for permissions
    /// Default: uses architecture-specific defaults
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// Gets the effective enabled state based on environment if not explicitly configured
    /// </summary>
    internal bool IsEnabled(string? environmentName)
    {
        if (Enabled.HasValue)
            return Enabled.Value;
            
        return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
    }
}