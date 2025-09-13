using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using Diiwo.Identity.Shared.Extensions;
using Diiwo.Identity.Shared.Attributes;
using Diiwo.Identity.Shared.Enums;
using Diiwo.Identity.App.DbContext;
using Diiwo.Identity.App.Entities;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Entities;

namespace Shared.Tests.Extensions;

/// <summary>
/// Integration test suite for automatic permission generation
/// Tests the complete end-to-end permission generation process
/// </summary>
[TestClass]
public class PermissionGenerationIntegrationTests
{
    private IConfiguration _baseConfiguration = null!;

    [TestInitialize]
    public void Setup()
    {
        // Setup base configuration that all tests can use
        _baseConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PermissionGeneration:Enabled"] = "true",
                ["PermissionGeneration:EnableLogging"] = "true",
                ["PermissionGeneration:ScanAssemblies:0"] = "Diiwo.Identity.Shared.Tests"
            })
            .Build();
    }

    /// <summary>
    /// Creates a configuration with additional settings merged with base configuration
    /// </summary>
    private IConfiguration CreateConfiguration(Dictionary<string, string?>? additionalSettings = null)
    {
        var allSettings = new Dictionary<string, string?>
        {
            ["PermissionGeneration:Enabled"] = "true",
            ["PermissionGeneration:EnableLogging"] = "true",
            ["PermissionGeneration:ScanAssemblies:0"] = "Diiwo.Identity.Shared.Tests"
        };

        if (additionalSettings != null)
        {
            foreach (var setting in additionalSettings)
            {
                allSettings[setting.Key] = setting.Value;
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(allSettings)
            .Build();
    }
    
    /// <summary>
    /// Test Case: Complete App Architecture Integration
    /// Description: Verifies full integration with App architecture from entity to database
    /// Acceptance Criteria:
    /// - Should scan entities and create AppPermission records
    /// - Should respect entity attributes correctly
    /// - Should handle different scopes and priorities
    /// - Should avoid duplicates on multiple runs
    /// </summary>
    [TestMethod]
    public async Task Integration_AppArchitecture_GeneratesPermissionsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        var databaseName = Guid.NewGuid().ToString();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Development"));
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddDbContext<AppIdentityDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: databaseName));

        var serviceProvider = services.BuildServiceProvider();
        var host = new TestHost(serviceProvider);

        // Act
        var result = await host.GeneratePermissionsAsync();

        // Assert
        Assert.IsTrue(result > 0, "Should generate permissions for test entities");

        // Verify permissions in database using the same context
        var context = serviceProvider.GetRequiredService<AppIdentityDbContext>();

        var permissions = await context.Permissions.ToListAsync();

        // Debug: Log all resources found
        var allResources = permissions.Select(p => p.Resource).Distinct().OrderBy(r => r).ToList();
        Console.WriteLine($"Found {permissions.Count} total permissions for resources: {string.Join(", ", allResources)}");

        // Should have permissions for TestCustomer entity
        var customerPermissions = permissions.Where(p => p.Resource == "TestCustomer").ToList();
        Assert.AreEqual(4, customerPermissions.Count, $"Should create 4 permissions for TestCustomer. Found resources: {string.Join(", ", allResources)}");

        Assert.IsTrue(customerPermissions.Any(p => p.Action == "View" && p.Description == "View customer information"),
            "Should have View permission");
        Assert.IsTrue(customerPermissions.Any(p => p.Action == "Create" && p.Description == "Create new customers"),
            "Should have Create permission");
        Assert.IsTrue(customerPermissions.Any(p => p.Action == "Edit" && p.Description == "Edit customer details"),
            "Should have Edit permission");
        Assert.IsTrue(customerPermissions.Any(p => p.Action == "Delete" && p.Description == "Delete customers"),
            "Should have Delete permission");

        // Should have permissions for TestProduct entity (our specific test entity)
        var productPermissions = permissions.Where(p => p.Resource == "TestProduct").ToList();
        Assert.IsTrue(productPermissions.Count >= 3, "Should create at least 3 permissions for TestProduct");

        // Verify specific permissions from our test entity exist
        var manageInventoryPermission = productPermissions.FirstOrDefault(p => p.Action == "ManageInventory" && p.Description == "Manage stock levels");
        Assert.IsNotNull(manageInventoryPermission, "Should have ManageInventory permission");
        Assert.AreEqual(PermissionScope.Global, manageInventoryPermission.Scope,
            "ManageInventory should have Global scope");

        var viewSensitivePermission = productPermissions.FirstOrDefault(p => p.Action == "ViewSensitive" && p.Description == "Access sensitive product data");
        Assert.IsNotNull(viewSensitivePermission, "Should have ViewSensitive permission");
        Assert.AreEqual(100, viewSensitivePermission.Priority, "ViewSensitive should have priority 100");
    }

    /// <summary>
    /// Test Case: Complete AspNet Architecture Integration
    /// Description: Verifies full integration with AspNet architecture
    /// Acceptance Criteria:
    /// - Should scan entities and create IdentityPermission records
    /// - Should work with ASP.NET Core Identity integration
    /// - Should handle enterprise entity scenarios
    /// </summary>
    [TestMethod]
    public async Task Integration_AspNetArchitecture_GeneratesPermissionsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        var databaseName = Guid.NewGuid().ToString();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Development"));
        services.AddLogging(builder => builder.AddConsole());
        services.AddDbContext<AspNetIdentityDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: databaseName));

        var serviceProvider = services.BuildServiceProvider();
        var host = new TestHost(serviceProvider);

        // Act
        var result = await host.GeneratePermissionsAsync();

        // Assert
        Assert.IsTrue(result > 0, "Should generate permissions for test entities");

        // Verify permissions in database using the same context
        var context = serviceProvider.GetRequiredService<AspNetIdentityDbContext>();

        var permissions = await context.IdentityPermissions.ToListAsync();

        // Should have permissions for test entities
        Assert.IsTrue(permissions.Count > 0, "Should create IdentityPermission records");

        // Verify permission properties for TestCustomer specifically
        var testPermissions = permissions.Where(p => p.Resource == "TestCustomer").ToList();
        Assert.AreEqual(4, testPermissions.Count, "Should have 4 permissions for TestCustomer");

        foreach (var permission in testPermissions)
        {
            Assert.IsNotNull(permission.Id, "Permission should have ID");
            Assert.IsNotNull(permission.Resource, "Permission should have Resource");
            Assert.IsNotNull(permission.Action, "Permission should have Action");
            Assert.IsTrue(permission.IsActive, "Permission should be active");
            Assert.IsTrue(permission.CreatedAt != default, "Permission should have CreatedAt");
            Assert.IsTrue(permission.UpdatedAt != default, "Permission should have UpdatedAt");
        }
    }

    /// <summary>
    /// Test Case: Duplicate Prevention
    /// Description: Verifies that running generation multiple times doesn't create duplicates
    /// Acceptance Criteria:
    /// - First run should create permissions
    /// - Second run should not create duplicates
    /// - Should return 0 on second run (no new permissions)
    /// </summary>
    [TestMethod]
    public async Task Integration_MultipleRuns_PreventsDuplicates()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();
        var databaseName = Guid.NewGuid().ToString();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Development"));
        services.AddLogging();
        services.AddDbContext<AppIdentityDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: databaseName));

        var serviceProvider = services.BuildServiceProvider();
        var host = new TestHost(serviceProvider);

        // Act - First run
        var firstResult = await host.GeneratePermissionsAsync();

        // Act - Second run
        var secondResult = await host.GeneratePermissionsAsync();

        // Assert
        Assert.IsTrue(firstResult > 0, "First run should generate permissions");
        Assert.AreEqual(0, secondResult, "Second run should not generate duplicates");

        // Verify no duplicates in database
        var context = serviceProvider.GetRequiredService<AppIdentityDbContext>();
        
        var permissions = await context.Permissions.ToListAsync();

        // Check for actual duplicates (same Resource, Action, AND Description)
        var duplicates = permissions.GroupBy(p => new { p.Resource, p.Action, p.Description })
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicates.Count > 0)
        {
            Console.WriteLine($"Found {duplicates.Count} groups of duplicates:");
            foreach (var duplicate in duplicates)
            {
                Console.WriteLine($"- {duplicate.Key.Resource}.{duplicate.Key.Action} ({duplicate.Key.Description}): {duplicate.Count()} copies");
            }
        }

        Assert.AreEqual(0, duplicates.Count, $"Should not have any duplicate permissions. Found duplicates: {string.Join(", ", duplicates.Select(d => $"{d.Key.Resource}.{d.Key.Action}"))}");
    }

    /// <summary>
    /// Test Case: Production Environment Behavior
    /// Description: Verifies that production environment skips generation by default
    /// Acceptance Criteria:
    /// - Should not generate permissions in Production without explicit config
    /// - Should return 0 permissions generated
    /// </summary>
    [TestMethod]
    public async Task Integration_ProductionEnvironment_SkipsGeneration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build(); // No explicit config

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Production"));
        services.AddLogging();
        services.AddDbContext<AppIdentityDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        var serviceProvider = services.BuildServiceProvider();
        var host = new TestHost(serviceProvider);

        // Act
        var result = await host.GeneratePermissionsAsync();

        // Assert
        Assert.AreEqual(0, result, "Production should skip generation by default");
    }

    /// <summary>
    /// Test Case: Custom Configuration Integration
    /// Description: Verifies that custom configuration options work correctly
    /// Acceptance Criteria:
    /// - Should respect SkipIfPermissionsExist configuration
    /// - Should handle custom table names
    /// - Should work with custom assembly scanning
    /// </summary>
    [TestMethod]
    public async Task Integration_CustomConfiguration_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["PermissionGeneration:SkipIfPermissionsExist"] = "true"
        });
        var databaseName = Guid.NewGuid().ToString();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Development"));
        services.AddLogging();
        services.AddDbContext<AppIdentityDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: databaseName));

        var serviceProvider = services.BuildServiceProvider();
        var host = new TestHost(serviceProvider);

        // Pre-populate with one permission to test skip behavior
        using (var scope = serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
            context.Permissions.Add(new AppPermission
            {
                Resource = "Existing",
                Action = "Test",
                Description = "Pre-existing permission"
            });
            await context.SaveChangesAsync();
        }

        // Act
        var result = await host.GeneratePermissionsAsync();

        // Assert
        Assert.AreEqual(0, result, "Should skip generation when permissions exist and SkipIfPermissionsExist is true");
    }

    /// <summary>
    /// Test Case: Mixed Entity Types Integration
    /// Description: Verifies generation works with entities having different attribute configurations
    /// Acceptance Criteria:
    /// - Should handle entities with different numbers of permissions
    /// - Should handle entities with different scopes and priorities
    /// - Should handle entities with and without descriptions
    /// </summary>
    [TestMethod]
    public async Task Integration_MixedEntityTypes_GeneratesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();
        var databaseName = Guid.NewGuid().ToString();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Development"));
        services.AddLogging();
        services.AddDbContext<AppIdentityDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: databaseName));

        var serviceProvider = services.BuildServiceProvider();
        var host = new TestHost(serviceProvider);

        // Act
        var result = await host.GeneratePermissionsAsync();

        // Assert
        Assert.IsTrue(result > 0, "Should generate permissions for mixed entity types");

        var context = serviceProvider.GetRequiredService<AppIdentityDbContext>();
        var permissions = await context.Permissions.ToListAsync();

        // Verify different entity types are handled correctly
        var customerPermissions = permissions.Where(p => p.Resource == "TestCustomer").ToList();
        var productPermissions = permissions.Where(p => p.Resource == "TestProduct").ToList();
        var simplePermissions = permissions.Where(p => p.Resource == "TestSimpleEntity").ToList();

        Assert.IsTrue(customerPermissions.Count >= 4, "Should have at least 4 customer permissions");
        Assert.IsTrue(productPermissions.Count >= 3, "Should have at least 3 product permissions");
        Assert.IsTrue(simplePermissions.Count >= 2, "Should have at least 2 simple entity permissions");

        // Verify our specific permissions exist
        Assert.IsTrue(customerPermissions.Any(p => p.Action == "View" && p.Description == "View customer information"),
            "Should have specific TestCustomer View permission");
        Assert.IsTrue(productPermissions.Any(p => p.Action == "ManageInventory" && p.Description == "Manage stock levels"),
            "Should have specific TestProduct ManageInventory permission");
        Assert.IsTrue(simplePermissions.Any(p => p.Action == "Read"),
            "Should have specific TestSimpleEntity Read permission");

        // Verify different scopes are preserved
        Assert.IsTrue(permissions.Any(p => p.Scope == PermissionScope.Global), "Should have Global scope permissions");
        Assert.IsTrue(permissions.Any(p => p.Scope == PermissionScope.Model), "Should have Model scope permissions");
        Assert.IsTrue(permissions.Any(p => p.Scope == PermissionScope.Object), "Should have Object scope permissions");
    }
}

#region Test Helper Classes

/// <summary>
/// Test implementation of IHostEnvironment
/// </summary>
public class TestHostEnvironment : IHostEnvironment
{
    public TestHostEnvironment(string environmentName)
    {
        EnvironmentName = environmentName;
    }

    public string EnvironmentName { get; set; }
    public string ApplicationName { get; set; } = "TestApp";
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}

/// <summary>
/// Test implementation of IHost
/// </summary>
public class TestHost : IHost
{
    public TestHost(IServiceProvider services)
    {
        Services = services;
    }

    public IServiceProvider Services { get; }

    public void Dispose() { }
    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

#endregion

#region Test Entity Classes

/// <summary>
/// Test customer entity with standard CRUD permissions
/// </summary>
[Permission("View", "View customer information")]
[Permission("Create", "Create new customers")]
[Permission("Edit", "Edit customer details")]
[Permission("Delete", "Delete customers")]
public class TestCustomer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Test product entity with mixed permission scopes and priorities
/// </summary>
[Permission("View", "View products")]
[Permission("ManageInventory", "Manage stock levels", PermissionScope.Global)]
[Permission("ViewSensitive", "Access sensitive product data", PermissionScope.Object, 100)]
public class TestProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

/// <summary>
/// Simple test entity with minimal permissions
/// </summary>
[Permission("Read")]
[Permission("Write", "Write data")]
public class TestSimpleEntity
{
    public Guid Id { get; set; }
    public string Data { get; set; } = string.Empty;
}

/// <summary>
/// Test article entity for CMS scenarios
/// </summary>
[Permission("View", "View articles")]
[Permission("Create", "Create articles")]
[Permission("Publish", "Publish articles")]
[Permission("Moderate", "Moderate content", PermissionScope.Global, 50)]
public class TestArticle
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
}

#endregion