using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Diiwo.Identity.Shared.Configuration;
using Diiwo.Identity.Shared.Attributes;
using Diiwo.Identity.Shared.Enums;
using Diiwo.Identity.App.DbContext;
using Diiwo.Identity.App.Entities;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Entities;

namespace Diiwo.Identity.Shared.Extensions;

/// <summary>
/// Extension methods for automatic permission generation from entity attributes.
/// Provides a simple one-line API for generating database permissions from [Permission] attributes.
/// </summary>
/// <remarks>
/// Environment behavior: Development auto-generates, Production skips (use migrations).
/// Supports both App and AspNet architectures by auto-detecting DbContext type.
/// </remarks>
public static class PermissionGenerationExtensions
{
    /// <summary>
    /// Generates database permissions automatically from entity attributes.
    /// Call this method during application startup: await app.GeneratePermissionsAsync();
    /// </summary>
    /// <param name="app">The host application instance</param>
    /// <returns>Number of permissions created in the database</returns>
    /// <remarks>
    /// Automatically detects App/AspNet architecture, scans assemblies for [Permission] attributes,
    /// and creates appropriate permission records. Prevents duplicates and handles environment-based configuration.
    /// </remarks>
    public static async Task<int> GeneratePermissionsAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        try
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            var environment = services.GetRequiredService<IHostEnvironment>();
            var logger = services.GetService<ILogger>();
            
            // Load configuration with intelligent defaults
            var options = new PermissionGenerationOptions();
            configuration.GetSection(PermissionGenerationOptions.SectionName).Bind(options);
            
            // Check if generation should run in this environment
            if (!options.IsEnabled(environment.EnvironmentName))
            {
                LogIfEnabled(logger, options, "Permission generation is disabled for environment: {Environment}", 
                    environment.EnvironmentName);
                return 0;
            }

            // Auto-detect architecture and generate permissions
            var appContext = services.GetService<AppIdentityDbContext>();
            if (appContext != null)
            {
                LogIfEnabled(logger, options, "Detected App architecture, generating AppPermission records");
                return await GenerateAppPermissionsAsync(appContext, options, logger);
            }

            var aspNetContext = services.GetService<AspNetIdentityDbContext>();
            if (aspNetContext != null)
            {
                LogIfEnabled(logger, options, "Detected AspNet architecture, generating IdentityPermission records");
                return await GenerateAspNetPermissionsAsync(aspNetContext, options, logger);
            }

            LogIfEnabled(logger, options, "No supported DbContext found (AppIdentityDbContext or AspNetIdentityDbContext)");
            return 0;
        }
        catch (Exception ex)
        {
            var logger = services.GetService<ILogger>();
            var environment = services.GetService<IHostEnvironment>();
            
            // In production, log and return 0 to prevent app startup failure
            if (environment?.EnvironmentName?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true)
            {
                logger?.LogWarning(ex, "Permission generation failed in production environment, continuing startup");
                return 0;
            }
            
            // In development, let the exception bubble up for debugging
            logger?.LogError(ex, "Permission generation failed during startup");
            throw;
        }
    }

    /// <summary>
    /// Generates permissions for App architecture using AppIdentityDbContext.
    /// </summary>
    /// <param name="context">The App architecture database context</param>
    /// <param name="options">Configuration options for permission generation</param>
    /// <param name="logger">Optional logger for diagnostic information</param>
    /// <returns>Number of AppPermission records created</returns>
    private static async Task<int> GenerateAppPermissionsAsync(
        AppIdentityDbContext context, 
        PermissionGenerationOptions options, 
        ILogger? logger)
    {
        // Skip if permissions already exist and configured to skip
        if (options.SkipIfPermissionsExist)
        {
            var hasExistingPermissions = await context.Permissions.AnyAsync();
            if (hasExistingPermissions)
            {
                LogIfEnabled(logger, options, "Skipping permission generation - AppPermission records already exist");
                return 0;
            }
        }

        var assemblies = GetAssembliesToScan(options);
        var totalGenerated = 0;

        LogIfEnabled(logger, options, "Scanning {AssemblyCount} assemblies for entities with Permission attributes", 
            assemblies.Length);

        foreach (var assembly in assemblies)
        {
            var entityTypes = GetEntityTypesWithPermissionAttributes(assembly);
            
            LogIfEnabled(logger, options, "Found {EntityCount} entities with permissions in assembly {AssemblyName}", 
                entityTypes.Length, assembly.GetName().Name);

            foreach (var entityType in entityTypes)
            {
                var permissionsGenerated = await CreateAppPermissionsForEntityAsync(context, entityType, options, logger);
                totalGenerated += permissionsGenerated;
            }
        }

        // Save all changes at once for better performance
        if (totalGenerated > 0)
        {
            await context.SaveChangesAsync();
            LogIfEnabled(logger, options, "Successfully created {Count} AppPermission records", totalGenerated);
        }

        return totalGenerated;
    }

    /// <summary>
    /// Generates permissions for AspNet architecture using AspNetIdentityDbContext.
    /// </summary>
    /// <param name="context">The AspNet architecture database context</param>
    /// <param name="options">Configuration options for permission generation</param>
    /// <param name="logger">Optional logger for diagnostic information</param>
    /// <returns>Number of IdentityPermission records created</returns>
    private static async Task<int> GenerateAspNetPermissionsAsync(
        AspNetIdentityDbContext context, 
        PermissionGenerationOptions options, 
        ILogger? logger)
    {
        // Skip if permissions already exist and configured to skip
        if (options.SkipIfPermissionsExist)
        {
            var hasExistingPermissions = await context.IdentityPermissions.AnyAsync();
            if (hasExistingPermissions)
            {
                LogIfEnabled(logger, options, "Skipping permission generation - IdentityPermission records already exist");
                return 0;
            }
        }

        var assemblies = GetAssembliesToScan(options);
        var totalGenerated = 0;

        LogIfEnabled(logger, options, "Scanning {AssemblyCount} assemblies for entities with Permission attributes", 
            assemblies.Length);

        foreach (var assembly in assemblies)
        {
            var entityTypes = GetEntityTypesWithPermissionAttributes(assembly);
            
            LogIfEnabled(logger, options, "Found {EntityCount} entities with permissions in assembly {AssemblyName}", 
                entityTypes.Length, assembly.GetName().Name);

            foreach (var entityType in entityTypes)
            {
                var permissionsGenerated = await CreateAspNetPermissionsForEntityAsync(context, entityType, options, logger);
                totalGenerated += permissionsGenerated;
            }
        }

        // Save all changes at once for better performance
        if (totalGenerated > 0)
        {
            await context.SaveChangesAsync();
            LogIfEnabled(logger, options, "Successfully created {Count} IdentityPermission records", totalGenerated);
        }

        return totalGenerated;
    }

    /// <summary>
    /// Creates AppPermission records for a specific entity type based on its PermissionAttribute decorations.
    /// </summary>
    /// <param name="context">The App architecture database context</param>
    /// <param name="entityType">The entity type to process</param>
    /// <param name="options">Configuration options</param>
    /// <param name="logger">Optional logger</param>
    /// <returns>Number of permissions created for this entity</returns>
    private static async Task<int> CreateAppPermissionsForEntityAsync(
        AppIdentityDbContext context, 
        Type entityType, 
        PermissionGenerationOptions options,
        ILogger? logger)
    {
        var permissionAttributes = GetPermissionAttributes(entityType);
        var resourceName = entityType.Name;
        var createdCount = 0;

        LogIfEnabled(logger, options, "Processing {PermissionCount} permissions for entity {EntityName}", 
            permissionAttributes.Length, resourceName);

        foreach (var permissionAttr in permissionAttributes)
        {
            // Check if permission already exists to prevent duplicates
            // Check both in database and in current context (tracking changes)
            var existingPermission = await context.Permissions
                .Where(p => p.Resource == resourceName && p.Action == permissionAttr.Action)
                .FirstOrDefaultAsync();

            // Also check if it's already been added to context in this transaction
            var existingInContext = context.ChangeTracker.Entries<AppPermission>()
                .Any(e => e.Entity.Resource == resourceName &&
                         e.Entity.Action == permissionAttr.Action &&
                         e.State == Microsoft.EntityFrameworkCore.EntityState.Added);

            if (existingPermission == null && !existingInContext)
            {
                var newPermission = new AppPermission
                {
                    Resource = resourceName,
                    Action = permissionAttr.Action,
                    Description = permissionAttr.Description,
                    Scope = permissionAttr.Scope,
                    Priority = permissionAttr.Priority
                };

                context.Permissions.Add(newPermission);
                createdCount++;

                LogIfEnabled(logger, options, 
                    "Created AppPermission: {Resource}.{Action} - {Description} (Scope: {Scope}, Priority: {Priority})",
                    resourceName, permissionAttr.Action, permissionAttr.Description, 
                    permissionAttr.Scope, permissionAttr.Priority);
            }
            else
            {
                var reason = existingPermission != null ? "exists in database" : "already added in this transaction";
                LogIfEnabled(logger, options,
                    "Skipped AppPermission: {Resource}.{Action} ({Reason})",
                    resourceName, permissionAttr.Action, reason);
            }
        }

        return createdCount;
    }

    /// <summary>
    /// Creates IdentityPermission records for a specific entity type based on its PermissionAttribute decorations.
    /// </summary>
    /// <param name="context">The AspNet architecture database context</param>
    /// <param name="entityType">The entity type to process</param>
    /// <param name="options">Configuration options</param>
    /// <param name="logger">Optional logger</param>
    /// <returns>Number of permissions created for this entity</returns>
    private static async Task<int> CreateAspNetPermissionsForEntityAsync(
        AspNetIdentityDbContext context, 
        Type entityType, 
        PermissionGenerationOptions options,
        ILogger? logger)
    {
        var permissionAttributes = GetPermissionAttributes(entityType);
        var resourceName = entityType.Name;
        var createdCount = 0;

        LogIfEnabled(logger, options, "Processing {PermissionCount} permissions for entity {EntityName}", 
            permissionAttributes.Length, resourceName);

        foreach (var permissionAttr in permissionAttributes)
        {
            // Check if permission already exists to prevent duplicates
            // Check both in database and in current context (tracking changes)
            var existingPermission = await context.IdentityPermissions
                .Where(p => p.Resource == resourceName && p.Action == permissionAttr.Action)
                .FirstOrDefaultAsync();

            // Also check if it's already been added to context in this transaction
            var existingInContext = context.ChangeTracker.Entries<IdentityPermission>()
                .Any(e => e.Entity.Resource == resourceName &&
                         e.Entity.Action == permissionAttr.Action &&
                         e.State == Microsoft.EntityFrameworkCore.EntityState.Added);

            if (existingPermission == null && !existingInContext)
            {
                var newPermission = new IdentityPermission
                {
                    Id = Guid.NewGuid(),
                    Resource = resourceName,
                    Action = permissionAttr.Action,
                    Description = permissionAttr.Description,
                    Scope = permissionAttr.Scope,
                    Priority = permissionAttr.Priority,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.IdentityPermissions.Add(newPermission);
                createdCount++;

                LogIfEnabled(logger, options, 
                    "Created IdentityPermission: {Resource}.{Action} - {Description} (Scope: {Scope}, Priority: {Priority})",
                    resourceName, permissionAttr.Action, permissionAttr.Description, 
                    permissionAttr.Scope, permissionAttr.Priority);
            }
            else
            {
                var reason = existingPermission != null ? "exists in database" : "already added in this transaction";
                LogIfEnabled(logger, options,
                    "Skipped IdentityPermission: {Resource}.{Action} ({Reason})",
                    resourceName, permissionAttr.Action, reason);
            }
        }

        return createdCount;
    }

    /// <summary>
    /// Determines which assemblies to scan for entities with Permission attributes.
    /// Uses configured assemblies or defaults to calling/entry assemblies.
    /// </summary>
    /// <param name="options">Configuration options that may specify custom assemblies</param>
    /// <returns>Array of assemblies to scan</returns>
    private static Assembly[] GetAssembliesToScan(PermissionGenerationOptions options)
    {
        if (options.ScanAssemblies?.Any() == true)
        {
            var assemblies = new List<Assembly>();
            
            foreach (var assemblyName in options.ScanAssemblies)
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    assemblies.Add(assembly);
                }
                catch (Exception)
                {
                    // Skip assemblies that can't be loaded
                    // In production, we don't want to fail startup due to missing assemblies
                }
            }
            
            return assemblies.ToArray();
        }

        // Default: scan the calling assembly and entry assembly
        var defaultAssemblies = new List<Assembly>();
        
        var callingAssembly = Assembly.GetCallingAssembly();
        if (callingAssembly != null)
            defaultAssemblies.Add(callingAssembly);
            
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null && !defaultAssemblies.Contains(entryAssembly))
            defaultAssemblies.Add(entryAssembly);

        return defaultAssemblies.ToArray();
    }

    /// <summary>
    /// Finds concrete classes in an assembly that are decorated with PermissionAttribute.
    /// </summary>
    /// <param name="assembly">The assembly to scan</param>
    /// <returns>Array of types with PermissionAttribute decorations</returns>
    private static Type[] GetEntityTypesWithPermissionAttributes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes()
                .Where(type => type.IsClass && 
                              !type.IsAbstract && 
                              type.GetCustomAttributes<PermissionAttribute>().Any())
                .ToArray();
        }
        catch (ReflectionTypeLoadException)
        {
            // Some assemblies may have types that can't be loaded
            // Return empty array to prevent startup failures
            return Array.Empty<Type>();
        }
    }

    /// <summary>
    /// Extracts all PermissionAttribute instances from a given entity type.
    /// </summary>
    /// <param name="entityType">The type to inspect</param>
    /// <returns>Array of PermissionAttribute instances</returns>
    private static PermissionAttribute[] GetPermissionAttributes(Type entityType)
    {
        return entityType.GetCustomAttributes<PermissionAttribute>().ToArray();
    }

    /// <summary>
    /// Logs messages only when logging is enabled in configuration.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="options">Configuration options to check if logging is enabled</param>
    /// <param name="message">The log message template</param>
    /// <param name="args">Arguments for the message template</param>
    private static void LogIfEnabled(ILogger? logger, PermissionGenerationOptions options, string message, params object[] args)
    {
        if (options.EnableLogging && logger != null)
        {
            logger.LogInformation(message, args);
        }
    }
}