using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Diiwo.Core.Extensions;
using Diiwo.Identity.App.DbContext;
using Diiwo.Identity.App.Application.Services;
using Diiwo.Identity.App.Abstractions.Services;
using Diiwo.Identity.Shared.Abstractions.Services;

namespace Diiwo.Identity.App.Extensions;

/// <summary>
/// APP ARCHITECTURE - Extension methods for configuring App Identity services
/// Simple, standalone configuration without ASP.NET Core Identity dependencies
/// Perfect for basic projects that need clean user management
/// </summary>
public static class AppIdentityServiceExtensions
{
    /// <summary>
    /// Add App Identity services with in-memory database (for development/testing)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="databaseName">In-memory database name</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAppIdentity(this IServiceCollection services, string databaseName = "AppIdentityDb")
    {
        // Configure DbContext with in-memory database
        services.AddDbContext<AppIdentityDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        // Register core services
        services.AddScoped<IAppUserService, AppUserService>();
        services.AddScoped<IAppPermissionService, AppPermissionService>();

        // Add logging
        services.AddLogging();

        return services;
    }

    /// <summary>
    /// Add App Identity services with SQL Server
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">SQL Server connection string</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAppIdentityWithSqlServer(this IServiceCollection services, string connectionString)
    {
        // Configure DbContext with SQL Server
        services.AddDbContext<AppIdentityDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register core services
        services.AddScoped<IAppUserService, AppUserService>();
        services.AddScoped<IAppPermissionService, AppPermissionService>();

        // Add logging
        services.AddLogging();

        return services;
    }

    /// <summary>
    /// Add App Identity services with PostgreSQL
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAppIdentityWithPostgreSQL(this IServiceCollection services, string connectionString)
    {
        // Configure DbContext with PostgreSQL
        services.AddDbContext<AppIdentityDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Register core services
        services.AddScoped<IAppUserService, AppUserService>();
        services.AddScoped<IAppPermissionService, AppPermissionService>();

        // Add logging
        services.AddLogging();

        return services;
    }

    /// <summary>
    /// Add App Identity services with custom DbContext configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="optionsAction">DbContext configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAppIdentity(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
    {
        // Configure DbContext with custom options
        services.AddDbContext<AppIdentityDbContext>(optionsAction);

        // Register core services
        services.AddScoped<IAppUserService, AppUserService>();
        services.AddScoped<IAppPermissionService, AppPermissionService>();

        // Add logging
        services.AddLogging();

        return services;
    }

    /// <summary>
    /// Add App Identity services reading configuration from IConfiguration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="connectionStringName">Connection string name (default: "DefaultConnection")</param>
    /// <param name="databaseProvider">Database provider (SqlServer, PostgreSQL, InMemory)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAppIdentity(
        this IServiceCollection services, 
        IConfiguration configuration, 
        string connectionStringName = "DefaultConnection",
        AppIdentityDatabaseProvider databaseProvider = AppIdentityDatabaseProvider.SqlServer)
    {
        var connectionString = configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrEmpty(connectionString) && databaseProvider != AppIdentityDatabaseProvider.InMemory)
        {
            throw new InvalidOperationException($"Connection string '{connectionStringName}' not found in configuration.");
        }

        return databaseProvider switch
        {
            AppIdentityDatabaseProvider.SqlServer => services.AddAppIdentityWithSqlServer(connectionString!),
            AppIdentityDatabaseProvider.PostgreSQL => services.AddAppIdentityWithPostgreSQL(connectionString!),
            AppIdentityDatabaseProvider.InMemory => services.AddAppIdentity("AppIdentityDb"),
            _ => throw new ArgumentOutOfRangeException(nameof(databaseProvider))
        };
    }

    /// <summary>
    /// Ensure App Identity database is created and seeded
    /// Call this in your Program.cs after building the app
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <param name="seedData">Whether to seed default data</param>
    /// <returns>Task</returns>
    public static async Task EnsureAppIdentityDatabaseAsync(this IServiceProvider serviceProvider, bool seedData = true)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
        
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        if (seedData)
        {
            await SeedDefaultAppDataAsync(context);
        }
    }

    /// <summary>
    /// Seed default data for App Identity
    /// </summary>
    private static async Task SeedDefaultAppDataAsync(AppIdentityDbContext context)
    {
        // Check if already seeded
        if (await context.Permissions.AnyAsync())
            return;

        // This will trigger the seeding configured in OnModelCreating
        await context.SaveChangesAsync();
    }
}

/// <summary>
/// Database provider options for App Identity
/// </summary>
public enum AppIdentityDatabaseProvider
{
    SqlServer,
    PostgreSQL, 
    InMemory
}