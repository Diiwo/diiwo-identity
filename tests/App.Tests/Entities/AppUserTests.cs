using Diiwo.Identity.App.Entities;

namespace Diiwo.Identity.App.Tests.Entities;

/// <summary>
/// Test suite for AppUser entity
/// Validates entity initialization, business logic, and computed properties
/// </summary>
[TestClass]
public class AppUserTests
{
    /// <summary>
    /// Test Case: AppUser Constructor Initialization
    /// Description: Verifies that AppUser constructor sets all default values correctly
    /// Acceptance Criteria:
    /// - User ID should be automatically generated and not empty (inherited from UserTrackedEntity)
    /// - IsActive should default to true for new users (inherited from BaseEntity)
    /// - EmailConfirmed should default to false requiring email verification
    /// - FailedLoginAttempts should start at 0
    /// - LockedUntil should be null (user not locked)
    /// - All navigation collections should be initialized as empty lists
    /// </summary>
    [TestMethod]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password"
        };

        // Assert
        Assert.AreNotEqual(Guid.Empty, user.Id, "User ID should be automatically generated");
        Assert.IsTrue(user.IsActive, "New users should be active by default");
        Assert.IsFalse(user.EmailConfirmed, "Email should require confirmation by default");
        Assert.AreEqual(0, user.FailedLoginAttempts, "Failed login attempts should start at 0");
        Assert.IsNull(user.LockedUntil, "New users should not be locked out");
        Assert.IsNotNull(user.UserSessions, "UserSessions collection should be initialized");
        Assert.IsNotNull(user.LoginHistory, "LoginHistory collection should be initialized");
        Assert.IsNotNull(user.UserPermissions, "UserPermissions collection should be initialized");
        Assert.IsNotNull(user.UserGroups, "UserGroups collection should be initialized");
    }

    /// <summary>
    /// Test Case: FullName Property with Complete Names
    /// Description: Verifies FullName property returns properly formatted full name when both first and last names are provided
    /// Acceptance Criteria:
    /// - Should combine FirstName and LastName with single space separator
    /// - Should handle typical name combinations correctly
    /// - Should maintain proper spacing and formatting
    /// </summary>
    [TestMethod]
    public void FullName_WithBothNames_ReturnsCorrectFormat()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var fullName = user.FullName;

        // Assert
        Assert.AreEqual("John Doe", fullName, "FullName should combine first and last name with space");
    }

    /// <summary>
    /// Test Case: FullName Property with First Name Only
    /// Description: Verifies FullName property handles cases where only FirstName is provided
    /// Acceptance Criteria:
    /// - Should return only the FirstName when LastName is null
    /// - Should not include extra spaces or formatting
    /// - Should handle null LastName gracefully
    /// </summary>
    [TestMethod]
    public void FullName_WithFirstNameOnly_ReturnsFirstName()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            FirstName = "John",
            LastName = null
        };

        // Act
        var fullName = user.FullName;

        // Assert
        Assert.AreEqual("John", fullName, "FullName should return FirstName when LastName is null");
    }

    /// <summary>
    /// Test Case: FullName Property with Last Name Only
    /// Description: Verifies FullName property handles cases where only LastName is provided
    /// Acceptance Criteria:
    /// - Should return only the LastName when FirstName is null
    /// - Should not include extra spaces or formatting
    /// - Should handle null FirstName gracefully
    /// </summary>
    [TestMethod]
    public void FullName_WithLastNameOnly_ReturnsLastName()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            FirstName = null,
            LastName = "Doe"
        };

        // Act
        var fullName = user.FullName;

        // Assert
        Assert.AreEqual("Doe", fullName, "FullName should return LastName when FirstName is null");
    }

    /// <summary>
    /// Test Case: FullName Property with No Names
    /// Description: Verifies FullName property handles cases where both names are null
    /// Acceptance Criteria:
    /// - Should return empty string when both FirstName and LastName are null
    /// - Should not return null or throw exception
    /// - Should handle edge case gracefully
    /// </summary>
    [TestMethod]
    public void FullName_WithNoNames_ReturnsEmpty()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            FirstName = null,
            LastName = null
        };

        // Act
        var fullName = user.FullName;

        // Assert
        Assert.AreEqual(string.Empty, fullName, "FullName should return empty string when both names are null");
    }

    /// <summary>
    /// Test Case: IsAccountLocked Property when User Not Locked
    /// Description: Verifies IsAccountLocked property returns false when user is not locked
    /// Acceptance Criteria:
    /// - Should return false when LockedUntil is null
    /// - Should return false when IsLocked (inherited) is false
    /// - Should handle normal user state correctly
    /// </summary>
    [TestMethod]
    public void IsAccountLocked_WhenNotLocked_ReturnsFalse()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            LockedUntil = null
        };

        // Act
        var isLocked = user.IsAccountLocked;

        // Assert
        Assert.IsFalse(isLocked, "IsAccountLocked should return false when user is not locked");
    }

    /// <summary>
    /// Test Case: IsAccountLocked Property when Lock Expired
    /// Description: Verifies IsAccountLocked property returns false when lock period has expired
    /// Acceptance Criteria:
    /// - Should return false when LockedUntil is in the past
    /// - Should handle automatic unlock correctly
    /// - Should use UTC time for comparison
    /// </summary>
    [TestMethod]
    public void IsAccountLocked_WhenLockExpired_ReturnsFalse()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            LockedUntil = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var isLocked = user.IsAccountLocked;

        // Assert
        Assert.IsFalse(isLocked, "IsAccountLocked should return false when lock period has expired");
    }

    /// <summary>
    /// Test Case: IsAccountLocked Property when Currently Locked
    /// Description: Verifies IsAccountLocked property returns true when user is currently locked
    /// Acceptance Criteria:
    /// - Should return true when LockedUntil is in the future
    /// - Should prevent user access during lock period
    /// - Should handle active lock correctly
    /// </summary>
    [TestMethod]
    public void IsAccountLocked_WhenCurrentlyLocked_ReturnsTrue()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            LockedUntil = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var isLocked = user.IsAccountLocked;

        // Assert
        Assert.IsTrue(isLocked, "IsAccountLocked should return true when user is currently locked");
    }

    /// <summary>
    /// Test Case: CanLogin Property with Valid User
    /// Description: Verifies CanLogin property returns true when user meets all login requirements
    /// Acceptance Criteria:
    /// - Should return true when IsActive, not locked, and email confirmed
    /// - Should allow login for valid users
    /// - Should check all required conditions
    /// </summary>
    [TestMethod]
    public void CanLogin_WithValidUser_ReturnsTrue()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            EmailConfirmed = true,
            LockedUntil = null
        };
        // IsActive defaults to true from base class

        // Act
        var canLogin = user.CanLogin;

        // Assert
        Assert.IsTrue(canLogin, "CanLogin should return true for valid user");
    }

    /// <summary>
    /// Test Case: CanLogin Property with Unconfirmed Email
    /// Description: Verifies CanLogin property returns false when email is not confirmed
    /// Acceptance Criteria:
    /// - Should return false when EmailConfirmed is false
    /// - Should prevent login without email verification
    /// - Should enforce email confirmation requirement
    /// </summary>
    [TestMethod]
    public void CanLogin_WithUnconfirmedEmail_ReturnsFalse()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            EmailConfirmed = false,
            LockedUntil = null
        };

        // Act
        var canLogin = user.CanLogin;

        // Assert
        Assert.IsFalse(canLogin, "CanLogin should return false when email is not confirmed");
    }

    /// <summary>
    /// Test Case: DisplayName Property with Full Name
    /// Description: Verifies DisplayName property returns full name when available
    /// Acceptance Criteria:
    /// - Should return FullName when first and/or last name are provided
    /// - Should prefer full name over email
    /// - Should handle name formatting correctly
    /// </summary>
    [TestMethod]
    public void DisplayName_WithFullName_ReturnsFullName()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var displayName = user.DisplayName;

        // Assert
        Assert.AreEqual("John Doe", displayName, "DisplayName should return full name when available");
    }

    /// <summary>
    /// Test Case: DisplayName Property without Names
    /// Description: Verifies DisplayName property falls back to email when no names provided
    /// Acceptance Criteria:
    /// - Should return Email when both FirstName and LastName are null
    /// - Should provide fallback display option
    /// - Should never return empty or null
    /// </summary>
    [TestMethod]
    public void DisplayName_WithoutNames_ReturnsEmail()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            FirstName = null,
            LastName = null
        };

        // Act
        var displayName = user.DisplayName;

        // Assert
        Assert.AreEqual("test@example.com", displayName, "DisplayName should return email when no names are provided");
    }
}