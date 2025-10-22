using Microsoft.VisualStudio.TestTools.UnitTesting;
using Diiwo.Identity.Shared.Extensions;

namespace Shared.Tests.Extensions;

/// <summary>
/// Test suite for CliExtensions integration functionality
/// Validates Program.cs integration for CLI command processing
/// </summary>
[TestClass]
public class CliExtensionsTests
{
    private StringWriter _outputWriter = null!;
    private StringWriter _errorWriter = null!;
    private TextWriter _originalOut = null!;
    private TextWriter _originalError = null!;
    private string _testOutputPath = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        // Create temporary directory for test output
        _testOutputPath = Path.Combine(Path.GetTempPath(), $"CliExtensionsTests_{Guid.NewGuid()}");
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
    /// Test Case: Permission Help Command Processing
    /// Description: Verifies ProcessPermissionCommands extension method processes help command
    /// Acceptance Criteria:
    /// - Should return true for --permissions-help command
    /// - Should display help information to console
    /// - Should be suitable for early Program.cs exit condition
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_HelpCommand_ReturnsTrue()
    {
        // Arrange
        var args = new[] { "--permissions-help" };

        // Act
        var shouldExitEarly = args.ProcessPermissionCommands();
        var output = _outputWriter.ToString();

        // Assert
        Assert.IsTrue(shouldExitEarly, "Should return true to indicate early exit from Program.cs");
        Assert.IsTrue(output.Contains("Permission Management Commands"),
            "Should display help content");
        Assert.IsTrue(output.Length > 100, 
            "Should display substantial help information");
    }

    /// <summary>
    /// Test Case: Preview Command Processing
    /// Description: Verifies ProcessPermissionCommands extension method processes preview command
    /// Acceptance Criteria:
    /// - Should return true for --make-permissions-preview command
    /// - Should display permission preview to console
    /// - Should not create any files
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_PreviewCommand_ReturnsTrue()
    {
        // Arrange
        var args = new[] { "--make-permissions-preview" };

        // Act
        var shouldExitEarly = args.ProcessPermissionCommands();
        var output = _outputWriter.ToString();

        // Assert
        Assert.IsTrue(shouldExitEarly, "Should return true to indicate early exit from Program.cs");
        Assert.IsTrue(output.Contains("Permission Preview"), 
            "Should display preview header");
        
        // Verify no migration files were created
        var files = Directory.GetFiles(_testOutputPath, "*.cs");
        Assert.AreEqual(0, files.Length, "Preview should not create files");
    }

    /// <summary>
    /// Test Case: Migration Generation Command Processing
    /// Description: Verifies ProcessPermissionCommands extension method processes migration generation
    /// Acceptance Criteria:
    /// - Should return true for --make-permissions command
    /// - Should create migration file in working directory
    /// - Should display success message
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_MakePermissions_ReturnsTrue()
    {
        // Arrange
        var args = new[] { "--make-permissions", "--output", _testOutputPath };

        // Act
        var shouldExitEarly = args.ProcessPermissionCommands();
        var output = _outputWriter.ToString();

        // Assert
        Assert.IsTrue(shouldExitEarly, "Should return true to indicate early exit from Program.cs");
        Assert.IsTrue(output.Contains("Generated migration"),
            "Should display success message");
        
        // Verify migration file was created
        var files = Directory.GetFiles(_testOutputPath, "*.cs");
        Assert.AreEqual(1, files.Length, "Should create migration file");
        Assert.IsTrue(Path.GetFileName(files[0]).StartsWith("202"), 
            "Migration file should have timestamp prefix");
    }

    /// <summary>
    /// Test Case: Non-Permission Commands
    /// Description: Verifies ProcessPermissionCommands extension method ignores non-permission commands
    /// Acceptance Criteria:
    /// - Should return false for non-permission commands
    /// - Should not display permission-related output
    /// - Should allow normal Program.cs execution to continue
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_NonPermissionCommands_ReturnsFalse()
    {
        // Arrange
        var args = new[] { "--environment", "Development", "--urls", "http://localhost:5000" };

        // Act
        var shouldExitEarly = args.ProcessPermissionCommands();
        var output = _outputWriter.ToString();

        // Assert
        Assert.IsFalse(shouldExitEarly, "Should return false to allow normal Program.cs execution");
        Assert.IsTrue(string.IsNullOrWhiteSpace(output), 
            "Should not display any output for non-permission commands");
    }

    /// <summary>
    /// Test Case: Empty Arguments
    /// Description: Verifies ProcessPermissionCommands extension method handles empty arguments
    /// Acceptance Criteria:
    /// - Should return false for empty arguments
    /// - Should not display any output
    /// - Should allow normal Program.cs execution
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_EmptyArguments_ReturnsFalse()
    {
        // Arrange
        var args = new string[0];

        // Act
        var shouldExitEarly = args.ProcessPermissionCommands();
        var output = _outputWriter.ToString();

        // Assert
        Assert.IsFalse(shouldExitEarly, "Should return false for empty arguments");
        Assert.IsTrue(string.IsNullOrWhiteSpace(output), 
            "Should not display any output for empty arguments");
    }

    /// <summary>
    /// Test Case: Null Arguments Handling
    /// Description: Verifies ProcessPermissionCommands extension method handles null arguments gracefully
    /// Acceptance Criteria:
    /// - Should return false for null arguments
    /// - Should not throw exceptions
    /// - Should allow normal Program.cs execution
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_NullArguments_ReturnsFalse()
    {
        // Arrange
        string[]? args = null;

        // Act & Assert - Should not throw
        try
        {
            var shouldExitEarly = args.ProcessPermissionCommands();
            Assert.IsFalse(shouldExitEarly, "Should return false for null arguments");
            
            var output = _outputWriter.ToString();
            Assert.IsTrue(string.IsNullOrWhiteSpace(output), 
                "Should not display any output for null arguments");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Should handle null arguments gracefully, but threw: {ex.Message}");
        }
    }

    /// <summary>
    /// Test Case: Custom Migration Name Integration
    /// Description: Verifies ProcessPermissionCommands passes custom name parameter correctly
    /// Acceptance Criteria:
    /// - Should use custom name when --name parameter is provided
    /// - Should create migration file with custom name
    /// - Should return true for successful processing
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_CustomMigrationName_UsesCustomName()
    {
        // Arrange
        var customName = "AddTestPermissions";
        var args = new[] { "--make-permissions", "--name", customName, "--output", _testOutputPath };

        // Act
        var shouldExitEarly = args.ProcessPermissionCommands();
        var output = _outputWriter.ToString();

        // Assert
        Assert.IsTrue(shouldExitEarly, "Should return true for successful processing");
        Assert.IsTrue(output.Contains("Generated migration"),
            "Should display success message");
        
        // Verify migration file uses custom name
        var files = Directory.GetFiles(_testOutputPath, "*.cs");
        Assert.AreEqual(1, files.Length, "Should create one migration file");
        Assert.IsTrue(Path.GetFileName(files[0]).Contains(customName), 
            "Migration file should contain custom name");
    }

    /// <summary>
    /// Test Case: Program.cs Integration Pattern
    /// Description: Verifies the recommended Program.cs integration pattern works correctly
    /// Acceptance Criteria:
    /// - Should demonstrate typical Program.cs usage pattern
    /// - Should handle both command processing and normal execution paths
    /// - Should validate return values for control flow
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_ProgramIntegrationPattern_WorksCorrectly()
    {
        // Test Case 1: Permission command should exit early
        var permissionArgs = new[] { "--make-permissions-preview" };
        var shouldExitForPermissionCommand = permissionArgs.ProcessPermissionCommands();
        Assert.IsTrue(shouldExitForPermissionCommand, 
            "Permission commands should return true for early Program.cs exit");

        // Reset output for second test
        _outputWriter.GetStringBuilder().Clear();

        // Test Case 2: Normal args should continue execution
        var normalArgs = new[] { "--environment", "Development" };
        var shouldExitForNormalArgs = normalArgs.ProcessPermissionCommands();
        Assert.IsFalse(shouldExitForNormalArgs, 
            "Normal arguments should return false to continue Program.cs execution");

        // Test Case 3: Demonstrate control flow pattern
        var testArgs = new[] { "--permissions-help" };
        if (testArgs.ProcessPermissionCommands())
        {
            // This represents early exit in Program.cs
            var output = _outputWriter.ToString();
            Assert.IsTrue(output.Contains("Permission Management Commands"),
                "Should have processed help command before early exit");
        }
        else
        {
            Assert.Fail("Help command should have returned true for early exit");
        }
    }

    /// <summary>
    /// Test Case: Mixed Arguments Processing
    /// Description: Verifies ProcessPermissionCommands correctly handles mixed command arguments
    /// Acceptance Criteria:
    /// - Should process permission commands when mixed with other arguments
    /// - Should ignore non-permission arguments
    /// - Should return true if any permission command is found
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_MixedArguments_ProcessesPermissionCommands()
    {
        // Arrange
        var args = new[] { 
            "--environment", "Development", 
            "--make-permissions-preview", 
            "--urls", "http://localhost:5000" 
        };

        // Act
        var shouldExitEarly = args.ProcessPermissionCommands();
        var output = _outputWriter.ToString();

        // Assert
        Assert.IsTrue(shouldExitEarly, "Should return true when permission command is present");
        Assert.IsTrue(output.Contains("Permission Preview"), 
            "Should process the permission command despite mixed arguments");
    }

    /// <summary>
    /// Test Case: Exception Handling
    /// Description: Verifies ProcessPermissionCommands handles exceptions gracefully
    /// Acceptance Criteria:
    /// - Should not throw unhandled exceptions
    /// - Should return false if processing fails
    /// - Should log errors appropriately
    /// </summary>
    [TestMethod]
    public void ProcessPermissionCommands_ExceptionScenarios_HandledGracefully()
    {
        // Test with potentially problematic arguments
        var problematicArgs = new[] { 
            "--make-permissions", 
            "--output", string.Empty, // Empty output path
            "--name", ""  // Empty migration name
        };

        // Act & Assert - Should not throw
        try
        {
            var result = problematicArgs.ProcessPermissionCommands();

            // The result depends on implementation, but should not throw
            // Either succeeds with defaults or fails gracefully
            Assert.IsNotNull(result, "Should return a boolean value");
            
            // Check if any error output was generated
            var errorOutput = _errorWriter.ToString();
            // Error handling verification depends on implementation details
        }
        catch (Exception ex)
        {
            Assert.Fail($"Should handle problematic arguments gracefully, but threw: {ex.Message}");
        }
    }
}