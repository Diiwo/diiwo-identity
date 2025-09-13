using Diiwo.Identity.AspNet.Entities;

namespace Diiwo.Identity.AspNet.Tests.Entities;

/// <summary>
/// Test suite for IdentityUser entity
/// Validates user entity logic, business rules, and AspNet Core Identity integration
/// </summary>
[TestClass]
public class IdentityUserTests
{
    /// <summary>
    /// Test Case: Identity User Creation with Required Properties
    /// Description: Verifies that IdentityUser can be created with minimal required data
    /// Acceptance Criteria:
    /// - User should be created with email and username
    /// - Default values should be applied correctly
    /// - AspNet Identity properties should be available
    /// </summary>
    [TestMethod]
    public void CreateIdentityUser_WithRequiredProperties_SetsDefaultsCorrectly()
    {
        // Arrange & Act
        var user = new IdentityUser
        {
            Email = "test@example.com",
            UserName = "testuser"
        };

        // Assert
        Assert.IsNotNull(user, "IdentityUser should be created successfully");
        Assert.AreEqual("test@example.com", user.Email, "Email should be set correctly");
        Assert.AreEqual("testuser", user.UserName, "Username should be set correctly");
        Assert.IsNotNull(user.Id, "User ID should be automatically generated");
        Assert.IsFalse(user.LockoutEnabled, "New users should not have lockout enabled by default");
        Assert.IsNull(user.LockoutEnd, "New users should not be locked out by default");
    }

    /// <summary>
    /// Test Case: Full Name Generation
    /// Description: Verifies that the full name property combines first and last names correctly
    /// Acceptance Criteria:
    /// - Full name should combine first and last names with space
    /// - Should handle null values gracefully
    /// - Should trim whitespace appropriately
    /// </summary>
    [TestMethod]
    public void GetFullName_WithFirstAndLastName_CombinesCorrectly()
    {
        // Arrange
        var user = new IdentityUser
        {
            Email = "test@example.com",
            UserName = "testuser",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var fullName = user.FullName;

        // Assert
        Assert.AreEqual("John Doe", fullName, "Full name should combine first and last names correctly");
    }

    /// <summary>
    /// Test Case: Full Name and Display Name with Missing Names
    /// Description: Verifies that name properties handle missing first or last names
    /// Acceptance Criteria:
    /// - FullName should return only available name when one is missing
    /// - FullName should handle null/empty values gracefully
    /// - DisplayName should return appropriate fallback when both names are missing
    /// </summary>
    [TestMethod]
    public void GetFullName_WithMissingNames_HandlesGracefully()
    {
        // Test with only first name
        var userFirstOnly = new IdentityUser
        {
            Email = "test@example.com",
            UserName = "testuser",
            FirstName = "John"
        };
        Assert.AreEqual("John", userFirstOnly.FullName, "Should return first name when last name is missing");

        // Test with only last name
        var userLastOnly = new IdentityUser
        {
            Email = "test@example.com",
            UserName = "testuser",
            LastName = "Doe"
        };
        Assert.AreEqual("Doe", userLastOnly.FullName, "Should return last name when first name is missing");

        // Test with no names
        var userNoNames = new IdentityUser
        {
            Email = "test@example.com",
            UserName = "testuser"
        };
        Assert.AreEqual("test@example.com", userNoNames.DisplayName, "Should return email when both names are missing (email is required in AspNet)");
    }

    /// <summary>
    /// Test Case: Cross-Architecture Migration Support
    /// Description: Verifies that optional foreign keys support migration from App architecture
    /// Acceptance Criteria:
    /// - AppUserId should be nullable for cross-architecture support
    /// - Should allow setting App architecture user reference
    /// - Migration relationships should work correctly
    /// </summary>
    [TestMethod]
    public void IdentityUser_CrossArchitectureMigration_SupportsAppUserReference()
    {
        // Arrange
        var appUserId = Guid.NewGuid();
        var user = new IdentityUser
        {
            Email = "migrated@example.com",
            UserName = "migrateduser",
            AppUserId = appUserId
        };

        // Assert
        Assert.AreEqual(appUserId, user.AppUserId, "AppUserId should support cross-architecture migration");
        Assert.IsNotNull(user.AppUserId, "AppUserId should be set for migrated users");
    }


    /// <summary>
    /// Test Case: Enterprise Audit Fields
    /// Description: Verifies that enterprise audit fields are properly maintained
    /// Acceptance Criteria:
    /// - Audit fields should track creation and modification
    /// - Should support user tracking for enterprise compliance
    /// - Timestamps should be in UTC
    /// </summary>
    [TestMethod]
    public void IdentityUser_EnterpriseAuditFields_TrackCorrectly()
    {
        // Arrange
        var user = new IdentityUser
        {
            Email = "audit@example.com",
            UserName = "audituser",
            CreatedBy = Guid.NewGuid(),
            UpdatedBy = Guid.NewGuid()
        };

        // Assert
        Assert.IsNotNull(user.CreatedBy, "CreatedBy should be trackable for audit");
        Assert.IsNotNull(user.UpdatedBy, "UpdatedBy should be trackable for audit");
        Assert.IsTrue(user.CreatedAt <= DateTime.UtcNow, "CreatedAt should be set to current or past time");
        Assert.IsTrue(user.UpdatedAt <= DateTime.UtcNow, "UpdatedAt should be set to current or past time");
    }

    /// <summary>
    /// Test Case: Navigation Properties Initialization
    /// Description: Verifies that navigation properties are properly initialized
    /// Acceptance Criteria:
    /// - Collections should be initialized to prevent null reference exceptions
    /// - Should support adding related entities
    /// - Navigation properties should follow EF Core conventions
    /// </summary>
    [TestMethod]
    public void IdentityUser_NavigationProperties_InitializedCorrectly()
    {
        // Arrange & Act
        var user = new IdentityUser
        {
            Email = "nav@example.com",
            UserName = "navuser"
        };

        // Assert
        Assert.IsNotNull(user.IdentityUserSessions, "IdentityUserSessions collection should be initialized");
        Assert.IsNotNull(user.IdentityLoginHistory, "IdentityLoginHistory collection should be initialized");
        Assert.IsNotNull(user.IdentityUserPermissions, "IdentityUserPermissions collection should be initialized");
        Assert.IsNotNull(user.IdentityUserGroups, "IdentityUserGroups collection should be initialized");
        Assert.AreEqual(0, user.IdentityUserSessions.Count, "IdentityUserSessions should be empty initially");
        Assert.AreEqual(0, user.IdentityLoginHistory.Count, "IdentityLoginHistory should be empty initially");
    }
}