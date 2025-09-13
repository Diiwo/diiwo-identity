using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Diiwo.Identity.Migration.Services;
using Diiwo.Identity.App.DbContext;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.App.Application.Services;
using Diiwo.Identity.AspNet.Application.Services;

namespace Diiwo.Identity.Migration.Extensions;

/// <summary>
/// MIGRATION EXTENSIONS - Easy setup for migration services
/// Configures both App and AspNet architectures for migration scenarios
/// </summary>
public static class MigrationServiceCollectionExtensions
{
    /// <summary>
    /// Adds migration services with both App and AspNet architectures configured
    /// </summary>
    public static IServiceCollection AddIdentityMigration(this IServiceCollection services, IConfiguration configuration, 
        string appConnectionStringName = "AppIdentityConnection", 
        string aspNetConnectionStringName = "AspNetIdentityConnection")
    {
        // Add both architectures using App and AspNet extension methods directly
        App.Extensions.ServiceCollectionExtensions.AddAppIdentity(services, configuration, appConnectionStringName);
        AspNet.Extensions.ServiceCollectionExtensions.AddAspNetIdentity(services, configuration, aspNetConnectionStringName);

        // Add migration service
        services.AddScoped<AppToAspNetMigrationService>();

        return services;
    }

    /// <summary>
    /// Adds migration services with custom connection strings
    /// </summary>
    public static IServiceCollection AddIdentityMigration(this IServiceCollection services, 
        string appConnectionString, 
        string aspNetConnectionString)
    {
        // Configure App architecture with custom connection string
        services.AddDbContext<AppIdentityDbContext>(options =>
        {
            options.UseNpgsql(appConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly("Diiwo.Identity.App");
                npgsql.CommandTimeout(30);
            });
        });

        // Add App services
        services.AddScoped<AppUserService>();
        services.AddScoped<AppPermissionService>();

        // Configure AspNet architecture with custom connection string
        services.AddDbContext<AspNetIdentityDbContext>(options =>
        {
            options.UseNpgsql(aspNetConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly("Diiwo.Identity.AspNet");
                npgsql.CommandTimeout(30);
            });
        });

        // Add AspNet services
        services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<AspNetIdentityDbContext>();
        services.AddScoped<AspNetUserService>();
        services.AddScoped<AspNetPermissionService>();

        // Add migration service
        services.AddScoped<AppToAspNetMigrationService>();

        return services;
    }

    /// <summary>
    /// Adds migration services with in-memory databases (for testing)
    /// </summary>
    public static IServiceCollection AddIdentityMigrationInMemory(this IServiceCollection services, 
        string appDatabaseName = "AppIdentityMigrationDb", 
        string aspNetDatabaseName = "AspNetIdentityMigrationDb")
    {
        // Add both architectures with in-memory databases
        App.Extensions.ServiceCollectionExtensions.AddAppIdentityInMemory(services, appDatabaseName);
        AspNet.Extensions.ServiceCollectionExtensions.AddAspNetIdentityInMemory(services, aspNetDatabaseName);

        // Add migration service
        services.AddScoped<AppToAspNetMigrationService>();

        return services;
    }

    /// <summary>
    /// Adds migration services for same-database scenario (different table prefixes)
    /// </summary>
    public static IServiceCollection AddIdentityMigrationSameDatabase(this IServiceCollection services, IConfiguration configuration, 
        string connectionStringName = "DefaultConnection")
    {
        var connectionString = configuration.GetConnectionString(connectionStringName);

        // Configure both architectures with same connection string
        services.AddDbContext<AppIdentityDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly("Diiwo.Identity.App");
                npgsql.CommandTimeout(30);
            });
        });

        // Add App services
        services.AddScoped<AppUserService>();
        services.AddScoped<AppPermissionService>();

        services.AddDbContext<AspNetIdentityDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly("Diiwo.Identity.AspNet");
                npgsql.CommandTimeout(30);
            });
        });

        // Add AspNet services
        services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<AspNetIdentityDbContext>();
        services.AddScoped<AspNetUserService>();
        services.AddScoped<AspNetPermissionService>();

        // Add migration service
        services.AddScoped<AppToAspNetMigrationService>();

        return services;
    }

    /// <summary>
    /// Performs automatic migration during application startup
    /// </summary>
    public static async Task<IServiceProvider> MigrateAppToAspNetAsync(this IServiceProvider serviceProvider, 
        bool createForeignKeyRelationships = true, 
        bool preserveAppData = true,
        bool validateAfterMigration = true)
    {
        using var scope = serviceProvider.CreateScope();
        var migrationService = scope.ServiceProvider.GetRequiredService<AppToAspNetMigrationService>();

        // Perform migration
        var result = await migrationService.MigrateUsersAsync();

        if (!result.IsSuccessful)
        {
            throw new InvalidOperationException($"Migration failed: {result.ErrorMessage}");
        }

        // Validate migration if requested
        if (validateAfterMigration && !result.IsSuccessful)
        {
            throw new InvalidOperationException($"Migration validation failed: {result.ErrorMessage}");
        }

        return serviceProvider;
    }
}