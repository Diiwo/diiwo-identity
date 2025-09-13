using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Diiwo.Core.Extensions;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.Shared.Abstractions.Services;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.AspNet.Extensions;

/// <summary>
///  ASPNET ARCHITECTURE - Extension methods for configuring AspNet Identity services
/// Enterprise configuration with full ASP.NET Core Identity integration
/// Advanced features, audit trails, and enterprise security
/// </summary>
public static class AspNetIdentityServiceExtensions
{
    /// <summary>
    /// Add AspNet Identity services with in-memory database (for development/testing)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="databaseName">In-memory database name</param>
    /// <param name="enableAppArchitectureForeignKeys">Enable foreign keys to App architecture</param>
    /// <param name="configureIdentity">Optional Identity configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAspNetIdentity(
        this IServiceCollection services, 
        string databaseName = "AspNetIdentityDb",
        bool enableAppArchitectureForeignKeys = false,
        Action<Microsoft.AspNetCore.Identity.IdentityOptions>? configureIdentity = null)
    {
        // Configure DbContext with in-memory database
        services.AddDbContext<AspNetIdentityDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        // Configure ASP.NET Core Identity
        ConfigureAspNetCoreIdentity(services, configureIdentity);

        // Register enterprise services
        services.AddScoped<IIdentityPermissionService, IdentityPermissionService>();

        // Add logging
        services.AddLogging();

        return services;
    }

    /// <summary>
    /// Add AspNet Identity services with SQL Server
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">SQL Server connection string</param>
    /// <param name="enableAppArchitectureForeignKeys">Enable foreign keys to App architecture</param>
    /// <param name="configureIdentity">Optional Identity configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAspNetIdentityWithSqlServer(
        this IServiceCollection services, 
        string connectionString,
        bool enableAppArchitectureForeignKeys = false,
        Action<Microsoft.AspNetCore.Identity.IdentityOptions>? configureIdentity = null)
    {
        // Configure DbContext with SQL Server
        services.AddDbContext<AspNetIdentityDbContext>(options =>
            options.UseSqlServer(connectionString),
            ServiceLifetime.Scoped,
            ServiceLifetime.Scoped);

        // Configure ASP.NET Core Identity
        ConfigureAspNetCoreIdentity(services, configureIdentity);

        // Register enterprise services
        services.AddScoped<IIdentityPermissionService>(provider =>
            new IdentityPermissionService(
                provider.GetRequiredService<AspNetIdentityDbContext>(),
                provider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Entities.IdentityUser>>(),
                provider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Entities.IdentityRole>>(),
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<IdentityPermissionService>>()));

        // Add logging
        services.AddLogging();

        return services;
    }

    /// <summary>
    /// Add AspNet Identity services with PostgreSQL
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <param name="enableAppArchitectureForeignKeys">Enable foreign keys to App architecture</param>
    /// <param name="configureIdentity">Optional Identity configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAspNetIdentityWithPostgreSQL(
        this IServiceCollection services, 
        string connectionString,
        bool enableAppArchitectureForeignKeys = false,
        Action<Microsoft.AspNetCore.Identity.IdentityOptions>? configureIdentity = null)
    {
        // Configure DbContext with PostgreSQL
        services.AddDbContext<AspNetIdentityDbContext>(options =>
            options.UseNpgsql(connectionString),
            ServiceLifetime.Scoped,
            ServiceLifetime.Scoped);

        // Configure ASP.NET Core Identity
        ConfigureAspNetCoreIdentity(services, configureIdentity);

        // Register enterprise services
        services.AddScoped<IIdentityPermissionService>(provider =>
            new IdentityPermissionService(
                provider.GetRequiredService<AspNetIdentityDbContext>(),
                provider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Entities.IdentityUser>>(),
                provider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Entities.IdentityRole>>(),
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<IdentityPermissionService>>()));

        // Add logging
        services.AddLogging();

        return services;
    }

    /// <summary>
    /// Add AspNet Identity services with custom DbContext configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="optionsAction">DbContext configuration action</param>
    /// <param name="enableAppArchitectureForeignKeys">Enable foreign keys to App architecture</param>
    /// <param name="configureIdentity">Optional Identity configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAspNetIdentity(
        this IServiceCollection services, 
        Action<DbContextOptionsBuilder> optionsAction,
        bool enableAppArchitectureForeignKeys = false,
        Action<Microsoft.AspNetCore.Identity.IdentityOptions>? configureIdentity = null)
    {
        // Configure DbContext with custom options
        services.AddDbContext<AspNetIdentityDbContext>(optionsAction);

        // Configure ASP.NET Core Identity
        ConfigureAspNetCoreIdentity(services, configureIdentity);

        // Register enterprise services
        services.AddScoped<IIdentityPermissionService>(provider =>
            new IdentityPermissionService(
                provider.GetRequiredService<AspNetIdentityDbContext>(),
                provider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Entities.IdentityUser>>(),
                provider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Entities.IdentityRole>>(),
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<IdentityPermissionService>>()));

        // Add logging
        services.AddLogging();

        return services;
    }

    /// <summary>
    /// Add AspNet Identity services reading configuration from IConfiguration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="connectionStringName">Connection string name (default: "DefaultConnection")</param>
    /// <param name="databaseProvider">Database provider</param>
    /// <param name="enableAppArchitectureForeignKeys">Enable foreign keys to App architecture</param>
    /// <param name="configureIdentity">Optional Identity configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAspNetIdentity(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "DefaultConnection",
        AspNetIdentityDatabaseProvider databaseProvider = AspNetIdentityDatabaseProvider.SqlServer,
        bool enableAppArchitectureForeignKeys = false,
        Action<Microsoft.AspNetCore.Identity.IdentityOptions>? configureIdentity = null)
    {
        var connectionString = configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrEmpty(connectionString) && databaseProvider != AspNetIdentityDatabaseProvider.InMemory)
        {
            throw new InvalidOperationException($"Connection string '{connectionStringName}' not found in configuration.");
        }

        return databaseProvider switch
        {
            AspNetIdentityDatabaseProvider.SqlServer => services.AddAspNetIdentityWithSqlServer(connectionString!, enableAppArchitectureForeignKeys, configureIdentity),
            AspNetIdentityDatabaseProvider.PostgreSQL => services.AddAspNetIdentityWithPostgreSQL(connectionString!, enableAppArchitectureForeignKeys, configureIdentity),
            AspNetIdentityDatabaseProvider.InMemory => services.AddAspNetIdentity("AspNetIdentityDb", enableAppArchitectureForeignKeys, configureIdentity),
            _ => throw new ArgumentOutOfRangeException(nameof(databaseProvider))
        };
    }

    /// <summary>
    /// Ensure AspNet Identity database is created and seeded
    /// Call this in your Program.cs after building the app
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <param name="seedData">Whether to seed default data</param>
    /// <param name="seedAdminUser">Whether to seed default admin user</param>
    /// <param name="adminEmail">Admin user email</param>
    /// <param name="adminPassword">Admin user password</param>
    /// <returns>Task</returns>
    public static async Task EnsureAspNetIdentityDatabaseAsync(
        this IServiceProvider serviceProvider, 
        bool seedData = true,
        bool seedAdminUser = true,
        string adminEmail = "admin@example.com",
        string adminPassword = "Admin123!")
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AspNetIdentityDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Entities.IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Entities.IdentityRole>>();
        
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        if (seedData)
        {
            await SeedDefaultAspNetDataAsync(context, userManager, roleManager, seedAdminUser, adminEmail, adminPassword);
        }
    }

    /// <summary>
    /// Configure ASP.NET Core Identity with enterprise defaults
    /// </summary>
    private static void ConfigureAspNetCoreIdentity(IServiceCollection services, Action<Microsoft.AspNetCore.Identity.IdentityOptions>? configureIdentity)
    {
        var identityBuilder = services.AddIdentity<Entities.IdentityUser, Entities.IdentityRole>(options =>
        {
            // Password requirements (enterprise-grade)
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredUniqueChars = 3;

            // Lockout settings (enterprise security)
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

            // Sign-in settings
            options.SignIn.RequireConfirmedEmail = true;
            options.SignIn.RequireConfirmedPhoneNumber = false;

            // Apply custom configuration if provided
            configureIdentity?.Invoke(options);
        })
        .AddEntityFrameworkStores<AspNetIdentityDbContext>()
        .AddDefaultTokenProviders();

        // Configure additional token providers for enterprise features
        identityBuilder.AddTokenProvider<DataProtectorTokenProvider<Entities.IdentityUser>>("Enterprise");
    }

    /// <summary>
    /// Seed default data for AspNet Identity
    /// </summary>
    private static async Task SeedDefaultAspNetDataAsync(
        AspNetIdentityDbContext context, 
        Microsoft.AspNetCore.Identity.UserManager<Entities.IdentityUser> userManager, 
        Microsoft.AspNetCore.Identity.RoleManager<Entities.IdentityRole> roleManager,
        bool seedAdminUser,
        string adminEmail,
        string adminPassword)
    {
        // Check if already seeded
        if (await context.IdentityPermissions.AnyAsync())
            return;

        // This will trigger the seeding configured in OnModelCreating
        await context.SaveChangesAsync();

        // Seed admin user if requested
        if (seedAdminUser)
        {
            await SeedAdminUserAsync(userManager, roleManager, adminEmail, adminPassword);
        }
    }

    /// <summary>
    /// Seed default admin user
    /// </summary>
    private static async Task SeedAdminUserAsync(
        Microsoft.AspNetCore.Identity.UserManager<Entities.IdentityUser> userManager, 
        Microsoft.AspNetCore.Identity.RoleManager<Entities.IdentityRole> roleManager,
        string adminEmail,
        string adminPassword)
    {
        // Check if admin user already exists
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser != null)
            return;

        // Ensure SuperAdmin role exists
        var superAdminRole = await roleManager.FindByNameAsync("SuperAdmin");
        if (superAdminRole == null)
            return;

        // Create admin user
        adminUser = new Entities.IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = "Super",
            LastName = "Admin"
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            // Add to SuperAdmin role
            await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
        }
    }
}

/// <summary>
/// Database provider options for AspNet Identity
/// </summary>
public enum AspNetIdentityDatabaseProvider
{
    SqlServer,
    PostgreSQL,
    InMemory
}

/// <summary>
/// Placeholder for IdentityPermissionService (will be implemented next)
/// </summary>
public class IdentityPermissionService : IIdentityPermissionService
{
    private readonly AspNetIdentityDbContext _context;
    private readonly Microsoft.AspNetCore.Identity.UserManager<Entities.IdentityUser> _userManager;
    private readonly Microsoft.AspNetCore.Identity.RoleManager<Entities.IdentityRole> _roleManager;
    private readonly Microsoft.Extensions.Logging.ILogger<IdentityPermissionService> _logger;

    public IdentityPermissionService(
        AspNetIdentityDbContext context,
        Microsoft.AspNetCore.Identity.UserManager<Entities.IdentityUser> userManager,
        Microsoft.AspNetCore.Identity.RoleManager<Entities.IdentityRole> roleManager,
        Microsoft.Extensions.Logging.ILogger<IdentityPermissionService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    // Implementation placeholder - will be completed next
    public Task<bool> UserHasPermissionAsync(Guid userId, string permissionName)
    {
        throw new NotImplementedException("AspNet Identity Permission Service implementation coming next...");
    }

    public Task<bool> UserHasPermissionAsync(string userEmail, string permissionName)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RoleHasPermissionAsync(Guid roleId, string permissionName)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RoleHasPermissionAsync(string roleName, string permissionName)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UserHasModelPermissionAsync(Guid userId, string permissionName, string modelType)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UserHasObjectPermissionAsync(Guid userId, string permissionName, Guid objectId, string objectType)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, bool>> UserHasPermissionsAsync(Guid userId, params string[] permissionNames)
    {
        throw new NotImplementedException();
    }

    public Task<bool> GrantUserPermissionAsync(Guid userId, string permissionName, DateTime? expiresAt = null, int priority = 100, Guid? grantedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RevokeUserPermissionAsync(Guid userId, string permissionName, Guid? revokedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> GrantRolePermissionAsync(Guid roleId, string permissionName, int priority = 0, Guid? grantedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> GrantRolePermissionAsync(string roleName, string permissionName, int priority = 0, Guid? grantedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RevokeRolePermissionAsync(Guid roleId, string permissionName, Guid? revokedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, bool>> BulkGrantUserPermissionsAsync(Guid userId, string[] permissionNames, DateTime? expiresAt = null, Guid? grantedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, bool>> BulkRevokeUserPermissionsAsync(Guid userId, string[] permissionNames, Guid? revokedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<(bool Success, Guid? GroupId)> CreateGroupAsync(string name, string? description = null, Guid? createdBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> GrantGroupPermissionAsync(Guid groupId, string permissionName, int priority = 50, Guid? grantedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RevokeGroupPermissionAsync(Guid groupId, string permissionName, Guid? revokedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> AddUserToGroupAsync(Guid userId, Guid groupId, Guid? addedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RemoveUserFromGroupAsync(Guid userId, Guid groupId, Guid? removedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<(Guid Id, string Name, string? Description, bool IsActive, int UserCount)> GetGroupDetailsAsync(Guid groupId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> GrantModelPermissionAsync(Guid userId, string permissionName, string modelType, int priority = 150, Guid? grantedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RevokeModelPermissionAsync(Guid userId, string permissionName, string modelType, Guid? revokedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> GrantObjectPermissionAsync(Guid userId, string permissionName, Guid objectId, string objectType, int priority = 200, Guid? grantedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RevokeObjectPermissionAsync(Guid userId, string permissionName, Guid objectId, string objectType, Guid? revokedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, bool>> BulkGrantObjectPermissionsAsync(Guid userId, string[] permissionNames, Guid[] objectIds, string objectType, Guid? grantedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<List<(string Permission, string Source, int Priority, DateTime? ExpiresAt)>> GetUserPermissionsWithSourceAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<List<string>> GetRolePermissionsAsync(Guid roleId)
    {
        throw new NotImplementedException();
    }

    public Task<List<string>> GetGroupPermissionsAsync(Guid groupId)
    {
        throw new NotImplementedException();
    }

    public Task<List<(Guid GroupId, string GroupName, DateTime JoinedAt, bool IsActive)>> GetUserGroupsAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, int>> GetPermissionUsageStatsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<List<(Guid UserId, string Email, string Source)>> GetUsersWithPermissionAsync(string permissionName)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreatePermissionAsync(string resource, string action, string? description = null, PermissionScope scope = PermissionScope.Global, Guid? createdBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdatePermissionAsync(string permissionName, string? newDescription = null, PermissionScope? newScope = null, Guid? updatedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeletePermissionAsync(string permissionName, Guid? deletedBy = null)
    {
        throw new NotImplementedException();
    }

    public Task<List<(string Name, string Resource, string Action, string? Description, PermissionScope Scope, EntityState State, DateTime CreatedAt)>> GetAllPermissionsAsync(bool includeDeleted = false)
    {
        throw new NotImplementedException();
    }

    public Task<List<(string Name, string Resource, string Action, string? Description)>> SearchPermissionsAsync(string? resource = null, string? action = null, PermissionScope? scope = null)
    {
        throw new NotImplementedException();
    }

    public Task<List<(DateTime Timestamp, string Action, string Permission, Guid? UserId, string? UserEmail, Guid? PerformedBy)>> GetPermissionAuditTrailAsync(Guid? userId = null, string? permissionName = null, DateTime? fromDate = null)
    {
        throw new NotImplementedException();
    }

    public Task<int> CleanupExpiredPermissionsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<(bool HasAccess, int RiskScore, string[] Reasons)> ValidateUserAccessAsync(Guid userId, string permissionName, string? ipAddress = null, string? userAgent = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SyncFromAppUserAsync(Guid identityUserId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UserHasPermissionInBothArchitecturesAsync(Guid identityUserId, string permissionName)
    {
        throw new NotImplementedException();
    }
}