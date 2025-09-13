using Diiwo.Identity.Migration.Models;

namespace Diiwo.Identity.Migration.Abstractions.Services;

/// <summary>
/// App to AspNet migration service interface
/// Handles complete migration from App architecture to AspNet architecture
/// Provides data migration, validation, and rollback capabilities
/// </summary>
public interface IAppToAspNetMigrationService
{
    /// <summary>
    /// Migrates all data from App architecture to AspNet architecture
    /// </summary>
    /// <param name="createForeignKeyRelationships">Whether to create foreign key relationships between architectures</param>
    /// <param name="preserveAppData">Whether to preserve original App data after migration</param>
    /// <returns>Migration result with success status and statistics</returns>
    Task<MigrationResult> MigrateAllAsync(bool createForeignKeyRelationships = true, bool preserveAppData = true);

    /// <summary>
    /// Migrates only users from App to AspNet architecture
    /// </summary>
    /// <param name="createForeignKeyRelationships">Whether to create foreign key relationships between architectures</param>
    /// <returns>Migration result with success status and statistics</returns>
    Task<MigrationResult> MigrateUsersAsync(bool createForeignKeyRelationships = true);

    /// <summary>
    /// Validates the migration process and data integrity
    /// </summary>
    /// <returns>Validation result with integrity check status and details</returns>
    Task<MigrationValidationResult> ValidateMigrationAsync();
}