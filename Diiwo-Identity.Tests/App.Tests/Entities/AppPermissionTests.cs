using Diiwo.Identity.App.Entities;
using Diiwo.Identity.Shared.Enums;

namespace App.Tests.Entities;

/// <summary>
/// Test suite for AppPermission entity
/// Validates permission definition, name formatting, and matching logic
/// </summary>
[TestClass]
public class AppPermissionTests
{
    /// <summary>
    /// Test Case: AppPermission Constructor Initialization
    /// Description: Verifies that AppPermission constructor sets all default values correctly
    /// Acceptance Criteria:
    /// - Permission ID should be automatically generated and not empty
    /// - Scope should default to Global
    /// - All navigation collections should be initialized as empty lists
    /// - Required properties should be properly handled
    /// </summary>
    [TestMethod]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var permission = new AppPermission
        {
            Resource = "TestResource",
            Action = "TestAction"
        };

        // Assert
        Assert.AreNotEqual(Guid.Empty, permission.Id, "Permission ID should be automatically generated");
        Assert.AreEqual(PermissionScope.Global, permission.Scope, "Scope should default to Global");
        Assert.IsNotNull(permission.RolePermissions, "RolePermissions collection should be initialized");
        Assert.IsNotNull(permission.GroupPermissions, "GroupPermissions collection should be initialized");
        Assert.IsNotNull(permission.UserPermissions, "UserPermissions collection should be initialized");
        Assert.IsNotNull(permission.ModelPermissions, "ModelPermissions collection should be initialized");
        Assert.IsNotNull(permission.ObjectPermissions, "ObjectPermissions collection should be initialized");
    }

    /// <summary>
    /// Test Case: Name Property Formatting
    /// Description: Verifies Name property returns correct Resource.Action format
    /// Acceptance Criteria:
    /// - Should combine Resource and Action with dot separator
    /// - Should maintain exact casing of Resource and Action
    /// - Should provide consistent naming convention
    /// </summary>
    [TestMethod]
    public void Name_ReturnsCorrectFormat()
    {
        // Arrange
        var permission = new AppPermission
        {
            Resource = "Document",
            Action = "Read"
        };

        // Act
        var name = permission.Name;

        // Assert
        Assert.AreEqual("Document.Read", name, "Name should combine Resource and Action with dot");
    }

    /// <summary>
    /// Test Case: Matches Method with Exact Match
    /// Description: Verifies Matches method returns true for exact permission name match
    /// Acceptance Criteria:
    /// - Should return true when permission name exactly matches
    /// - Should handle standard permission naming
    /// - Should be case-sensitive for exact matches
    /// </summary>
    [TestMethod]
    public void Matches_WithMatchingName_ReturnsTrue()
    {
        // Arrange
        var permission = new AppPermission
        {
            Resource = "Document",
            Action = "Read"
        };

        // Act
        var matches = permission.Matches("Document.Read");

        // Assert
        Assert.IsTrue(matches, "Matches should return true for exact permission name");
    }

    /// <summary>
    /// Test Case: Matches Method with Case Insensitive Match
    /// Description: Verifies Matches method handles case insensitive comparison
    /// Acceptance Criteria:
    /// - Should return true when permission name matches ignoring case
    /// - Should handle various case combinations
    /// - Should be flexible for user input
    /// </summary>
    [TestMethod]
    public void Matches_WithMatchingNameDifferentCase_ReturnsTrue()
    {
        // Arrange
        var permission = new AppPermission
        {
            Resource = "Document",
            Action = "Read"
        };

        // Act
        var matches = permission.Matches("document.read");

        // Assert
        Assert.IsTrue(matches, "Matches should return true ignoring case differences");
    }

    /// <summary>
    /// Test Case: Matches Method with Non-Matching Name
    /// Description: Verifies Matches method returns false for different permission names
    /// Acceptance Criteria:
    /// - Should return false when permission name does not match
    /// - Should distinguish between different permissions
    /// - Should handle permission validation correctly
    /// </summary>
    [TestMethod]
    public void Matches_WithNonMatchingName_ReturnsFalse()
    {
        // Arrange
        var permission = new AppPermission
        {
            Resource = "Document",
            Action = "Read"
        };

        // Act
        var matches = permission.Matches("Document.Write");

        // Assert
        Assert.IsFalse(matches, "Matches should return false for different permission names");
    }

    /// <summary>
    /// Test Case: Permission with Different Scopes
    /// Description: Verifies permission can be created with different scope levels
    /// Acceptance Criteria:
    /// - Should accept Global, Model, and Object scopes
    /// - Should maintain scope setting correctly
    /// - Should support permission hierarchy
    /// </summary>
    [TestMethod]
    public void Permission_WithDifferentScopes_SetsCorrectly()
    {
        // Arrange & Act
        var globalPermission = new AppPermission
        {
            Resource = "System",
            Action = "Admin",
            Scope = PermissionScope.Global
        };

        var modelPermission = new AppPermission
        {
            Resource = "User",
            Action = "Create",
            Scope = PermissionScope.Model
        };

        var objectPermission = new AppPermission
        {
            Resource = "Document",
            Action = "Edit",
            Scope = PermissionScope.Object
        };

        // Assert
        Assert.AreEqual(PermissionScope.Global, globalPermission.Scope, "Global permission scope should be set correctly");
        Assert.AreEqual(PermissionScope.Model, modelPermission.Scope, "Model permission scope should be set correctly");
        Assert.AreEqual(PermissionScope.Object, objectPermission.Scope, "Object permission scope should be set correctly");
    }
}