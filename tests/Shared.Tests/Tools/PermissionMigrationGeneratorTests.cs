using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Diiwo.Identity.Shared.Tools;
using Diiwo.Identity.Shared.Attributes;
using Diiwo.Identity.Shared.Enums;

namespace Shared.Tests.Tools;

/// <summary>
/// Test suite for PermissionMigrationGenerator
/// Validates automatic migration file generation from entity attributes
/// </summary>
[TestClass]
public class PermissionMigrationGeneratorTests
{
    private string _testOutputPath = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        // Create temporary directory for test output
        _testOutputPath = Path.Combine(Path.GetTempPath(), $"PermissionGeneratorTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputPath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clean up test directory
        if (Directory.Exists(_testOutputPath))
        {
            Directory.Delete(_testOutputPath, true);
        }
    }

    /// <summary>
    /// Test Case: Basic Migration Generation
    /// Description: Verifies that a migration file is generated with basic entity permissions
    /// Acceptance Criteria:
    /// - Should create migration file with correct timestamp format
    /// - Should include all entity permissions in comments
    /// - Should include both Up and Down methods
    /// - Should provide architecture-specific code templates
    /// </summary>
    [TestMethod]
    public void GenerateMigration_BasicEntities_CreatesValidMigrationFile()
    {
        // Arrange
        var assemblies = new[] { Assembly.GetExecutingAssembly() };

        // Act
        var migrationPath = PermissionMigrationGenerator.GenerateMigration(
            "TestBasicPermissions", 
            _testOutputPath, 
            assemblies
        );

        // Assert
        Assert.IsTrue(File.Exists(migrationPath), "Migration file should be created");
        Assert.IsTrue(Path.GetFileName(migrationPath).Contains("TestBasicPermissions"), 
            "Migration file should contain specified name");
        Assert.IsTrue(Path.GetFileName(migrationPath).StartsWith("202"), 
            "Migration file should start with timestamp");

        var migrationContent = File.ReadAllText(migrationPath);

        // Verify essential migration structure
        Assert.IsTrue(migrationContent.Contains("using Microsoft.EntityFrameworkCore.Migrations;"), 
            "Should include required using statements");
        Assert.IsTrue(migrationContent.Contains("using Diiwo.Identity.Shared.Extensions;"), 
            "Should include extension methods using");
        Assert.IsTrue(migrationContent.Contains("public partial class"), 
            "Should define migration class");
        Assert.IsTrue(migrationContent.Contains("protected override void Up"), 
            "Should include Up method");
        Assert.IsTrue(migrationContent.Contains("protected override void Down"), 
            "Should include Down method");

        // Verify architecture-specific templates
        Assert.IsTrue(migrationContent.Contains("GenerateAppEntityPermissions"), 
            "Should include App architecture template");
        Assert.IsTrue(migrationContent.Contains("GenerateAspNetEntityPermissions"), 
            "Should include AspNet architecture template");

        // Verify test entity permissions are documented
        Assert.IsTrue(migrationContent.Contains("TestEntityBasic"), 
            "Should document TestEntityBasic permissions");
        Assert.IsTrue(migrationContent.Contains("Read"), 
            "Should document Read permission");
        Assert.IsTrue(migrationContent.Contains("Write"), 
            "Should document Write permission");
    }

    /// <summary>
    /// Test Case: Complex Permissions with Scopes and Priorities
    /// Description: Verifies complex permission attributes are properly documented
    /// Acceptance Criteria:
    /// - Should include scope information in comments
    /// - Should include priority information in comments
    /// - Should handle multiple permissions per entity
    /// - Should format complex permission descriptions properly
    /// </summary>
    [TestMethod]
    public void GenerateMigration_ComplexPermissions_DocumentsAllAttributes()
    {
        // Arrange
        var assemblies = new[] { Assembly.GetExecutingAssembly() };

        // Act
        var migrationPath = PermissionMigrationGenerator.GenerateMigration(
            "TestComplexPermissions", 
            _testOutputPath, 
            assemblies
        );

        // Assert
        var migrationContent = File.ReadAllText(migrationPath);

        // Verify complex entity documentation
        Assert.IsTrue(migrationContent.Contains("TestEntityComplex"), 
            "Should document TestEntityComplex permissions");
        Assert.IsTrue(migrationContent.Contains("Scope: Global"), 
            "Should document Global scope permissions");
        Assert.IsTrue(migrationContent.Contains("Priority: 100"), 
            "Should document custom priority permissions");
        Assert.IsTrue(migrationContent.Contains("Scope: Object"), 
            "Should document Object scope permissions");
        Assert.IsTrue(migrationContent.Contains("Priority: 50"), 
            "Should document different priority values");
    }

    /// <summary>
    /// Test Case: No Entities Found
    /// Description: Verifies behavior when no entities with permissions are found
    /// Acceptance Criteria:
    /// - Should return empty string when no entities found
    /// - Should not create migration file
    /// - Should handle empty assembly gracefully
    /// </summary>
    [TestMethod]
    public void GenerateMigration_NoEntitiesFound_ReturnsEmptyString()
    {
        // Arrange
        var emptyAssembly = typeof(string).Assembly; // System assembly with no Permission attributes

        // Act
        var migrationPath = PermissionMigrationGenerator.GenerateMigration(
            "NoEntitiesTest", 
            _testOutputPath, 
            new[] { emptyAssembly }
        );

        // Assert
        Assert.AreEqual(string.Empty, migrationPath, "Should return empty string when no entities found");
        
        var files = Directory.GetFiles(_testOutputPath, "*.cs");
        Assert.AreEqual(0, files.Length, "Should not create any migration files");
    }

    /// <summary>
    /// Test Case: Custom Migration Name and Path
    /// Description: Verifies custom naming and output path functionality
    /// Acceptance Criteria:
    /// - Should use custom migration name in file name
    /// - Should create file in specified output directory
    /// - Should maintain timestamp prefix format
    /// </summary>
    [TestMethod]
    public void GenerateMigration_CustomNameAndPath_UsesCorrectNaming()
    {
        // Arrange
        var customName = "AddUserManagementPermissions";
        var customPath = Path.Combine(_testOutputPath, "CustomMigrations");
        Directory.CreateDirectory(customPath);
        var assemblies = new[] { Assembly.GetExecutingAssembly() };

        // Act
        var migrationPath = PermissionMigrationGenerator.GenerateMigration(
            customName, 
            customPath, 
            assemblies
        );

        // Assert
        Assert.IsTrue(File.Exists(migrationPath), "Migration file should exist");
        Assert.IsTrue(migrationPath.StartsWith(customPath), "Should be in custom output directory");
        Assert.IsTrue(Path.GetFileName(migrationPath).Contains(customName), 
            "File name should contain custom migration name");
        
        // Verify file content has custom class name
        var migrationContent = File.ReadAllText(migrationPath);
        Assert.IsTrue(migrationContent.Contains($"_{customName} : Migration"), 
            "Class name should include custom migration name");
    }

    /// <summary>
    /// Test Case: Entity Scanning Functionality
    /// Description: Verifies the ScanForPermissionEntities method works correctly
    /// Acceptance Criteria:
    /// - Should find entities with Permission attributes
    /// - Should ignore entities without Permission attributes
    /// - Should handle multiple assemblies
    /// - Should return correct entity information
    /// </summary>
    [TestMethod]
    public void ScanForPermissionEntities_MultipleAssemblies_FindsCorrectEntities()
    {
        // Arrange
        var assemblies = new[] { Assembly.GetExecutingAssembly() };

        // Act
        var entityPermissions = PermissionMigrationGenerator.ScanForPermissionEntities(assemblies);

        // Assert
        Assert.IsTrue(entityPermissions.Length > 0, "Should find entities with permissions");
        
        var basicEntity = entityPermissions.FirstOrDefault(e => e.EntityName == "TestEntityBasic");
        Assert.IsNotNull(basicEntity, "Should find TestEntityBasic");
        Assert.AreEqual(2, basicEntity.Permissions.Length, "TestEntityBasic should have 2 permissions");
        Assert.IsTrue(basicEntity.Permissions.Any(p => p.Action == "Read"), "Should have Read permission");
        Assert.IsTrue(basicEntity.Permissions.Any(p => p.Action == "Write"), "Should have Write permission");

        var complexEntity = entityPermissions.FirstOrDefault(e => e.EntityName == "TestEntityComplex");
        Assert.IsNotNull(complexEntity, "Should find TestEntityComplex");
        Assert.IsTrue(complexEntity.Permissions.Length >= 3, "TestEntityComplex should have multiple permissions");
        
        var globalPermission = complexEntity.Permissions.FirstOrDefault(p => p.Scope == PermissionScope.Global);
        Assert.IsNotNull(globalPermission, "Should have permission with Global scope");
        
        var priorityPermission = complexEntity.Permissions.FirstOrDefault(p => p.Priority == 100);
        Assert.IsNotNull(priorityPermission, "Should have permission with custom priority");
    }

    /// <summary>
    /// Test Case: Preview Permissions Functionality
    /// Description: Verifies the PreviewPermissions method provides correct output
    /// Acceptance Criteria:
    /// - Should display entity names and permission counts
    /// - Should show permission details with scopes and priorities
    /// - Should handle entities with no permissions gracefully
    /// - Should provide summary information
    /// </summary>
    [TestMethod]
    public void PreviewPermissions_ValidEntities_DisplaysCorrectInformation()
    {
        // Arrange
        var assemblies = new[] { Assembly.GetExecutingAssembly() };
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            PermissionMigrationGenerator.PreviewPermissions(assemblies);

            // Assert
            var output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Permission Preview"), "Should show preview header");
            Assert.IsTrue(output.Contains("TestEntityBasic"), "Should list TestEntityBasic");
            Assert.IsTrue(output.Contains("TestEntityComplex"), "Should list TestEntityComplex");
            Assert.IsTrue(output.Contains("Summary:"), "Should show summary");
            Assert.IsTrue(output.Contains("permissions across"), "Should show permission count summary");
            Assert.IsTrue(output.Contains("Scope:"), "Should show scope information");
            Assert.IsTrue(output.Contains("Priority:"), "Should show priority information");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// Test Case: Empty Assembly Handling
    /// Description: Verifies graceful handling of assemblies with no permission entities
    /// Acceptance Criteria:
    /// - Should return empty array for assemblies without permissions
    /// - Should not throw exceptions for problematic assemblies
    /// - Should handle ReflectionTypeLoadException gracefully
    /// </summary>
    [TestMethod]
    public void ScanForPermissionEntities_EmptyAssembly_ReturnsEmptyArray()
    {
        // Arrange
        var systemAssembly = typeof(string).Assembly; // No Permission attributes expected

        // Act
        var entityPermissions = PermissionMigrationGenerator.ScanForPermissionEntities(new[] { systemAssembly });

        // Assert
        Assert.IsNotNull(entityPermissions, "Should return non-null array");
        Assert.AreEqual(0, entityPermissions.Length, "Should return empty array for assemblies without permissions");
    }
}

#region Test Entity Classes

/// <summary>
/// Basic test entity with simple permissions
/// </summary>
[Permission("Read", "Read basic entity data")]
[Permission("Write", "Write basic entity data")]
internal class TestEntityBasic
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Complex test entity with various permission configurations
/// </summary>
[Permission("View", "View complex entity", PermissionScope.Model, 0)]
[Permission("Manage", "Manage complex entity globally", PermissionScope.Global, 100)]
[Permission("Edit", "Edit specific instance", PermissionScope.Object, 50)]
[Permission("Delete", "Delete complex entity")]
internal class TestEntityComplex
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Entity without permissions (should be ignored)
/// </summary>
internal class TestEntityWithoutPermissions
{
    public Guid Id { get; set; }
    public string Data { get; set; } = string.Empty;
}

#endregion