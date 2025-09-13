using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Diiwo.Identity.App.DbContext;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.Migration.Models;
using Diiwo.Identity.Migration.Services;
using Diiwo.Identity.Migration.Abstractions.Services;

namespace Diiwo.Identity.Migration;

/// <summary>
/// MIGRATION EXTENSIONS - Easy-to-use extensions for App → AspNet migration
/// Provides convenient methods to register and execute migrations
/// </summary>
public static class MigrationExtensions
{
    /// <summary>
    /// Add migration services to dependency injection
    /// Call this after configuring both App and AspNet architectures
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAppToAspNetMigration(this IServiceCollection services)
    {
        services.AddScoped<IAppToAspNetMigrationService, AppToAspNetMigrationService>();
        return services;
    }

    /// <summary>
    /// Execute migration from App to AspNet architecture
    /// Call this method in your Program.cs or migration console app
    /// </summary>
    /// <param name="serviceProvider">Service provider with both App and AspNet contexts configured</param>
    /// <param name="enableForeignKeys">Enable foreign key relationships back to App users</param>
    /// <param name="migratePasswords">Migrate password hashes (risky - ensure compatibility)</param>
    /// <param name="logProgress">Whether to log detailed progress</param>
    /// <returns>Migration result</returns>
    public static async Task<MigrationResult> ExecuteAppToAspNetMigrationAsync(
        this IServiceProvider serviceProvider,
        bool enableForeignKeys = true,
        bool migratePasswords = false,
        bool logProgress = true)
    {
        using var scope = serviceProvider.CreateScope();
        var migrationService = scope.ServiceProvider.GetRequiredService<IAppToAspNetMigrationService>();
        
        if (logProgress)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppToAspNetMigrationService>>();
            logger.LogInformation("🚀 Starting App → AspNet migration with enableForeignKeys={EnableForeignKeys}, migratePasswords={MigratePasswords}", 
                enableForeignKeys, migratePasswords);
        }

        var result = await migrationService.MigrateUsersAsync();

        if (logProgress)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppToAspNetMigrationService>>();
            if (result.IsSuccessful)
            {
                logger.LogInformation("Migration completed successfully in {Duration}", result.Duration);
                logger.LogInformation(result.ToString());
            }
            else
            {
                logger.LogError("Migration failed: {Error}", result.ErrorMessage);
            }
        }

        return result;
    }

    /// <summary>
    /// Validate that both App and AspNet contexts are properly configured
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <returns>True if both contexts are available and accessible</returns>
    public static async Task<bool> ValidateMigrationPrerequisitesAsync(this IServiceProvider serviceProvider)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            
            // Test App context
            var appContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
            await appContext.Database.CanConnectAsync();
            
            // Test AspNet context
            var aspNetContext = scope.ServiceProvider.GetRequiredService<AspNetIdentityDbContext>();
            await aspNetContext.Database.CanConnectAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetService<ILogger<AppToAspNetMigrationService>>();
            logger?.LogError(ex, "Migration prerequisites validation failed");
            return false;
        }
    }

    /// <summary>
    /// Get migration statistics without executing migration
    /// Useful for showing user what will be migrated before starting
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <returns>Migration preview with counts</returns>
    public static async Task<MigrationPreview> GetMigrationPreviewAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var appContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
        
        var preview = new MigrationPreview
        {
            UsersToMigrate = await appContext.Users.CountAsync(),
            PermissionsToMigrate = await appContext.Permissions.CountAsync(),
            GroupsToMigrate = await appContext.Groups.CountAsync(),
            UserGroupsToMigrate = 0, // Groups relationship handled separately
            UserPermissionsToMigrate = await appContext.UserPermissions.CountAsync(),
            GroupPermissionsToMigrate = await appContext.GroupPermissions.CountAsync(),
            ModelPermissionsToMigrate = await appContext.ModelPermissions.CountAsync(),
            ObjectPermissionsToMigrate = await appContext.ObjectPermissions.CountAsync(),
            SessionsToMigrate = await appContext.UserSessions.CountAsync(),
            LoginHistoryToMigrate = await appContext.LoginHistory.CountAsync()
        };

        return preview;
    }
}

/// <summary>
/// Migration preview with item counts
/// </summary>
public class MigrationPreview
{
    public int UsersToMigrate { get; set; }
    public int PermissionsToMigrate { get; set; }
    public int GroupsToMigrate { get; set; }
    public int UserGroupsToMigrate { get; set; }
    public int UserPermissionsToMigrate { get; set; }
    public int GroupPermissionsToMigrate { get; set; }
    public int ModelPermissionsToMigrate { get; set; }
    public int ObjectPermissionsToMigrate { get; set; }
    public int SessionsToMigrate { get; set; }
    public int LoginHistoryToMigrate { get; set; }

    public int TotalItemsToMigrate => UsersToMigrate + PermissionsToMigrate + GroupsToMigrate + 
                                    UserGroupsToMigrate + UserPermissionsToMigrate + GroupPermissionsToMigrate + 
                                    ModelPermissionsToMigrate + ObjectPermissionsToMigrate + 
                                    SessionsToMigrate + LoginHistoryToMigrate;

    public override string ToString()
    {
        return $"📊 Migration Preview:\n" +
               $"   👥 Users: {UsersToMigrate}\n" +
               $"   🔒 Permissions: {PermissionsToMigrate}\n" +
               $"   👥 Groups: {GroupsToMigrate}\n" +
               $"   🔗 User-Groups: {UserGroupsToMigrate}\n" +
               $"   🔗 User Permissions: {UserPermissionsToMigrate}\n" +
               $"   🔗 Group Permissions: {GroupPermissionsToMigrate}\n" +
               $"   🎯 Model Permissions: {ModelPermissionsToMigrate}\n" +
               $"   🎯 Object Permissions: {ObjectPermissionsToMigrate}\n" +
               $"   📊 Sessions: {SessionsToMigrate}\n" +
               $"   📈 Login History: {LoginHistoryToMigrate}\n" +
               $"   📦 Total Items: {TotalItemsToMigrate}";
    }
}