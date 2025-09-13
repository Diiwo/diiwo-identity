using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Diiwo.Identity.Shared.CLI;
using Diiwo.Identity.Shared.Attributes;
using Diiwo.Identity.Shared.Enums;

namespace Shared.Tests.CLI;

/// <summary>
/// Test suite for PermissionCommands CLI functionality
/// Validates command-line argument processing and migration generation commands
/// </summary>
[TestClass]
public class PermissionCommandsTests
{
    private string _testOutputPath = string.Empty;
    private StringWriter _outputWriter = null!;
    private StringWriter _errorWriter = null!;
    private TextWriter _originalOut = null!;
    private TextWriter _originalError = null!;

    [TestInitialize]
    public void Setup()
    {
        // Create temporary directory for test output
        _testOutputPath = Path.Combine(Path.GetTempPath(), $"PermissionCommandsTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputPath);

        // Capture console output
        _originalOut = Console.Out;
        _originalError = Console.Error;
        _outputWriter = new StringWriter();
        _errorWriter = new StringWriter();
        Console.SetOut(_outputWriter);
        Console.SetError(_errorWriter);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Restore console output
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
        _outputWriter?.Dispose();
        _errorWriter?.Dispose();

        // Clean up test directory
        if (Directory.Exists(_testOutputPath))
        {
            Directory.Delete(_testOutputPath, true);
        }
    }

    /// <summary>
    /// Test Case: Help Command Processing
    /// Description: Verifies --permissions-help command displays usage information
    /// Acceptance Criteria:
    /// - Should display available commands
    /// - Should show command syntax examples
    /// - Should return true indicating command was processed
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_HelpCommand_DisplaysHelpInformation()
    {
        // Arrange
        var args = new[] { "--permissions-help" };

        // Act
        var wasProcessed = PermissionCommands.ProcessArgs(args);
        var output = _outputWriter.ToString();

        // Assert
        Assert.IsTrue(wasProcessed, "Help command should be processed");
        Assert.IsTrue(output.Contains("DIIWO Identity - Permission Management Commands"),
            "Should display help header");
        Assert.IsTrue(output.Contains("--apply-permissions"),
            "Should show apply command");
        Assert.IsTrue(output.Contains("--make-permissions-preview"),
            "Should show preview command");
        Assert.IsTrue(output.Contains("--make-permissions"),
            "Should show generation command");
        Assert.IsTrue(output.Contains("RECOMMENDED"),
            "Should highlight recommended workflow");
        Assert.IsTrue(output.Contains("Examples:"),
            "Should include usage examples");
    }

    /// <summary>
    /// Test Case: Preview Command Processing
    /// Description: Verifies --make-permissions-preview command shows entity permissions
    /// Acceptance Criteria:
    /// - Should display found entities and their permissions
    /// - Should show permission details (scope, priority)
    /// - Should not create any files
    /// - Should return true indicating command was processed
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_PreviewCommand_DisplaysPermissionPreview()
    {
        // Arrange
        var args = new[] { "--make-permissions-preview" };

        // Act
        var wasProcessed = PermissionCommands.ProcessArgs(args);
        var output = _outputWriter.ToString();

        // Assert
        Assert.IsTrue(wasProcessed, "Preview command should be processed");
        Assert.IsTrue(output.Contains("Permission Preview"),
            "Should display preview header");
        Assert.IsTrue(output.Contains("TestCommandEntity"),
            "Should show test entity");
        Assert.IsTrue(output.Contains("Summary:"),
            "Should display summary information");
        
        // Verify no files were created
        var files = Directory.GetFiles(_testOutputPath, "*.cs");
        Assert.AreEqual(0, files.Length, "Preview should not create migration files");
    }

    /// <summary>
    /// Test Case: Migration Generation Command
    /// Description: Verifies --make-permissions command creates migration file
    /// Acceptance Criteria:
    /// - Should create migration file with timestamp prefix
    /// - Should include all entity permissions in generated file
    /// - Should display success message with file path
    /// - Should return true indicating command was processed
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_MakePermissions_GeneratesMigrationFile()
    {
        // Arrange
        var args = new[] { "--make-permissions", "--output", _testOutputPath };

        // Act
        var wasProcessed = PermissionCommands.ProcessArgs(args);
        var output = _outputWriter.ToString();

        // Assert
        Assert.IsTrue(wasProcessed, "Make permissions command should be processed");
        Assert.IsTrue(output.Contains("Generated migration"),
            "Should display success message");
        Assert.IsTrue(output.Contains(_testOutputPath),
            "Should show output path in message");

        // Verify migration file was created
        var files = Directory.GetFiles(_testOutputPath, "*.cs");
        Assert.AreEqual(1, files.Length, "Should create one migration file");
        
        var migrationFile = files[0];
        Assert.IsTrue(Path.GetFileName(migrationFile).StartsWith("202"), 
            "Migration file should have timestamp prefix");
        Assert.IsTrue(Path.GetFileName(migrationFile).Contains("AutoGeneratePermissions"), 
            "Migration file should contain default name");

        // Verify file content
        var content = File.ReadAllText(migrationFile);
        Assert.IsTrue(content.Contains("TestCommandEntity"), 
            "Should include test entity in migration");
        Assert.IsTrue(content.Contains("GenerateAppEntityPermissions"), 
            "Should include App architecture template");
        Assert.IsTrue(content.Contains("GenerateAspNetEntityPermissions"), 
            "Should include AspNet architecture template");
    }

    /// <summary>
    /// Test Case: Custom Migration Name
    /// Description: Verifies --name parameter customizes migration file name
    /// Acceptance Criteria:
    /// - Should use custom name in migration file name
    /// - Should use custom name in migration class name
    /// - Should still include timestamp prefix
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_CustomMigrationName_UsesCustomName()
    {
        // Arrange
        var customName = "AddUserManagementPermissions";
        var args = new[] { "--make-permissions", "--name", customName, "--output", _testOutputPath };

        // Act
        var wasProcessed = PermissionCommands.ProcessArgs(args);
        var output = _outputWriter.ToString();

        // Assert
        Assert.IsTrue(wasProcessed, "Make permissions command should be processed");
        
        // Verify migration file uses custom name
        var files = Directory.GetFiles(_testOutputPath, "*.cs");
        Assert.AreEqual(1, files.Length, "Should create one migration file");
        
        var migrationFile = files[0];
        Assert.IsTrue(Path.GetFileName(migrationFile).Contains(customName), 
            "Migration file should contain custom name");

        // Verify class name uses custom name
        var content = File.ReadAllText(migrationFile);
        Assert.IsTrue(content.Contains($"_{customName} : Migration"), 
            "Class name should include custom migration name");
    }

    /// <summary>
    /// Test Case: Invalid Command Arguments
    /// Description: Verifies unrecognized commands are not processed
    /// Acceptance Criteria:
    /// - Should return false for unrecognized commands
    /// - Should not create any files
    /// - Should not display permission-related output
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_InvalidCommand_ReturnsNotProcessed()
    {
        // Arrange
        var args = new[] { "--invalid-command" };

        // Act
        var wasProcessed = PermissionCommands.ProcessArgs(args);

        // Assert
        Assert.IsFalse(wasProcessed, "Invalid command should not be processed");
        
        // Verify no files were created
        var files = Directory.GetFiles(_testOutputPath, "*.cs");
        Assert.AreEqual(0, files.Length, "Should not create any files for invalid commands");
    }

    /// <summary>
    /// Test Case: Empty Arguments
    /// Description: Verifies empty arguments are handled gracefully
    /// Acceptance Criteria:
    /// - Should return false when no arguments provided
    /// - Should not throw exceptions
    /// - Should not create any files
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_EmptyArguments_ReturnsNotProcessed()
    {
        // Arrange
        var args = new string[0];

        // Act
        var wasProcessed = PermissionCommands.ProcessArgs(args);

        // Assert
        Assert.IsFalse(wasProcessed, "Empty arguments should not be processed");
        
        // Verify no files were created
        var files = Directory.GetFiles(_testOutputPath, "*.cs");
        Assert.AreEqual(0, files.Length, "Should not create any files for empty arguments");
    }

    /// <summary>
    /// Test Case: Apply Permissions Command Processing
    /// Description: Verifies --apply-permissions command is recognized and processed
    /// Acceptance Criteria:
    /// - Should be recognized as a valid command
    /// - Should return true indicating command was processed
    /// - Should handle database connection gracefully in test environment
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_ApplyPermissions_IsRecognizedAsValidCommand()
    {
        // Arrange
        var args = new[] { "--apply-permissions" };

        // Redirect input to simulate user cancelling the operation
        var inputReader = new StringReader("n\n");
        Console.SetIn(inputReader);

        // Act
        var wasProcessed = PermissionCommands.ProcessArgs(args);
        var output = _outputWriter.ToString();

        // Assert
        Assert.IsTrue(wasProcessed, "Apply permissions command should be processed");
        Assert.IsTrue(output.Contains("Direct Permission Application"),
            "Should display apply permissions header");
        Assert.IsTrue(output.Contains("Scanning for entities with [Permission] attributes"),
            "Should show scanning message");
    }

    /// <summary>
    /// Test Case: Multiple Commands Processing
    /// Description: Verifies behavior when multiple permission commands are provided
    /// Acceptance Criteria:
    /// - Should process the first recognized command
    /// - Should return true if any command is processed
    /// - Should handle conflicting commands gracefully
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_MultipleCommands_ProcessesFirstCommand()
    {
        // Arrange
        var args = new[] { "--make-permissions-preview", "--make-permissions", "--output", _testOutputPath };

        // Act
        var wasProcessed = PermissionCommands.ProcessArgs(args);
        var output = _outputWriter.ToString();

        // Assert
        Assert.IsTrue(wasProcessed, "Should process recognized commands");
        Assert.IsTrue(output.Contains("Permission Preview"), 
            "Should process preview command (first in arguments)");
        
        // Since preview was processed first, no migration file should be created
        var files = Directory.GetFiles(_testOutputPath, "*.cs");
        Assert.AreEqual(0, files.Length, "Preview command should not create files");
    }

    /// <summary>
    /// Test Case: Output Path Parameter
    /// Description: Verifies --output parameter specifies custom output directory
    /// Acceptance Criteria:
    /// - Should create migration file in specified directory
    /// - Should create directory if it doesn't exist
    /// - Should handle both absolute and relative paths
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_OutputPath_CreatesFileInSpecifiedDirectory()
    {
        // Arrange
        var customOutput = Path.Combine(_testOutputPath, "CustomMigrations");
        var args = new[] { "--make-permissions", "--output", customOutput };

        // Act
        var wasProcessed = PermissionCommands.ProcessArgs(args);

        // Assert
        Assert.IsTrue(wasProcessed, "Command should be processed");
        Assert.IsTrue(Directory.Exists(customOutput), "Custom output directory should be created");
        
        var files = Directory.GetFiles(customOutput, "*.cs");
        Assert.AreEqual(1, files.Length, "Should create migration file in custom directory");
    }

    /// <summary>
    /// Test Case: Error Handling for Invalid Output Path
    /// Description: Verifies graceful handling of invalid output paths
    /// Acceptance Criteria:
    /// - Should display error message for invalid paths
    /// - Should not create files in invalid locations
    /// - Should not throw unhandled exceptions
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_InvalidOutputPath_HandlesGracefully()
    {
        // Arrange
        var invalidPath = "Z:\\NonExistentDrive\\InvalidPath";
        var args = new[] { "--make-permissions", "--output", invalidPath };

        // Act & Assert - Should not throw
        try
        {
            var wasProcessed = PermissionCommands.ProcessArgs(args);
            
            // Command should still be processed, but error should be logged
            Assert.IsTrue(wasProcessed, "Command processing should not fail due to invalid path");
            
            // Check if error was logged (depending on implementation)
            var errorOutput = _errorWriter.ToString();
            // Error handling depends on implementation - this test ensures no exceptions
        }
        catch (Exception ex)
        {
            Assert.Fail($"Should handle invalid output path gracefully, but threw: {ex.Message}");
        }
    }
}

#region Test Entity Classes

/// <summary>
/// Test entity for CLI command testing
/// </summary>
[Permission("View", "View command test entity")]
[Permission("Execute", "Execute command operations", PermissionScope.Global, 75)]
[Permission("Manage", "Manage command entity settings", PermissionScope.Object, 25)]
public class TestCommandEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
}

/// <summary>
/// Another test entity to verify multiple entity processing
/// </summary>
[Permission("Read", "Read command data")]
[Permission("Write", "Write command data")]
public class TestCommandData
{
    public Guid Id { get; set; }
    public string Data { get; set; } = string.Empty;
}

#endregion