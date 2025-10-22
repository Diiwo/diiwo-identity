using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Diiwo.Identity.App.DbContext;
using Diiwo.Identity.App.Application.Services;
using Diiwo.Identity.App.Entities;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.App.Tests.Services;

/// <summary>
/// Test suite for AppUserService
/// Validates user management operations, authentication logic, and business rules
/// </summary>
[TestClass]
public class AppUserServiceTests
{
    private AppIdentityDbContext _context = null!;
    private AppUserService _userService = null!;
    private Mock<ILogger<AppUserService>> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppIdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppIdentityDbContext(options);
        _mockLogger = new Mock<ILogger<AppUserService>>();
        _userService = new AppUserService(_context, _mockLogger.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    /// <summary>
    /// Test Case: User Retrieval by ID
    /// Description: Verifies that users can be successfully retrieved by their unique identifier
    /// Acceptance Criteria:
    /// - Valid user ID should return the corresponding user
    /// - All user properties should be correctly loaded
    /// - Service should handle database queries efficiently
    /// </summary>
    [TestMethod]
    public async Task GetUserByIdAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByIdAsync(user.Id);

        // Assert
        Assert.IsNotNull(result, "User should be successfully retrieved by ID");
        Assert.AreEqual(user.Id, result.Id, "Retrieved user should have matching ID");
        Assert.AreEqual("test@example.com", result.Email, "Retrieved user should have correct email");
    }

    /// <summary>
    /// Test Case: Invalid User ID Handling
    /// Description: Verifies that service handles non-existent user IDs gracefully
    /// Acceptance Criteria:
    /// - Non-existent user ID should return null
    /// - Service should not throw exceptions for invalid IDs
    /// - Database queries should be optimized for non-existent records
    /// </summary>
    [TestMethod]
    public async Task GetUserByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _userService.GetUserByIdAsync(Guid.NewGuid());

        // Assert
        Assert.IsNull(result, "Non-existent user ID should return null");
    }

    /// <summary>
    /// Test Case: User Retrieval by Email
    /// Description: Verifies that users can be found using their email address
    /// Acceptance Criteria:
    /// - Valid email should return the corresponding user
    /// - Email lookup should be case-insensitive or consistent
    /// - Service should handle email-based queries efficiently
    /// </summary>
    [TestMethod]
    public async Task GetUserByEmailAsync_WithValidEmail_ReturnsUser()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByEmailAsync("test@example.com");

        // Assert
        Assert.IsNotNull(result, "User should be successfully retrieved by email");
        Assert.AreEqual("test@example.com", result.Email, "Retrieved user should have correct email");
    }

    /// <summary>
    /// Test Case: Invalid Email Handling
    /// Description: Verifies that service handles non-existent email addresses gracefully
    /// Acceptance Criteria:
    /// - Non-existent email should return null
    /// - Service should not throw exceptions for invalid emails
    /// - Email validation should be consistent with business rules
    /// </summary>
    [TestMethod]
    public async Task GetUserByEmailAsync_WithInvalidEmail_ReturnsNull()
    {
        // Act
        var result = await _userService.GetUserByEmailAsync("notfound@example.com");

        // Assert
        Assert.IsNull(result, "Non-existent email should return null");
    }

    /// <summary>
    /// Test Case: User Creation with Valid Data
    /// Description: Verifies that new users can be created with proper validation and defaults
    /// Acceptance Criteria:
    /// - User should be created with provided information
    /// - Default values should be applied correctly
    /// - User should be persisted to database
    /// - Business rules should be enforced during creation
    /// </summary>
    [TestMethod]
    public async Task CreateUserAsync_WithValidData_CreatesUser()
    {
        // Act
        var result = await _userService.CreateUserAsync(
            "newuser@example.com", 
            "hashed-password", 
            "John", 
            "Doe");

        // Assert
        Assert.IsNotNull(result, "User should be successfully created");
        Assert.AreEqual("newuser@example.com", result.Email, "Created user should have correct email");
        Assert.AreEqual("John", result.FirstName, "Created user should have correct first name");
        Assert.AreEqual("Doe", result.LastName, "Created user should have correct last name");
        Assert.IsTrue(result.IsActive, "New users should be active by default");

        var userInDb = await _context.Users.FindAsync(result.Id);
        Assert.IsNotNull(userInDb, "User should be persisted to database");
    }

    /// <summary>
    /// Test Case: User Information Update
    /// Description: Verifies that existing user information can be modified correctly
    /// Acceptance Criteria:
    /// - User properties should be updated with new values
    /// - Changes should be persisted to database
    /// - Audit fields should be maintained properly
    /// - Business rules should be validated during updates
    /// </summary>
    [TestMethod]
    public async Task UpdateUserAsync_WithValidUser_UpdatesUser()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password",
            FirstName = "John"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        user.FirstName = "Jane";

        // Act
        var result = await _userService.UpdateUserAsync(user);

        // Assert
        Assert.IsTrue(result);

        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.IsNotNull(updatedUser);
        Assert.AreEqual("Jane", updatedUser.FirstName);
    }

    [TestMethod]
    public async Task DeleteUserAsync_WithValidId_DeletesUser()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.DeleteUserAsync(user.Id);

        // Assert
        Assert.IsTrue(result);

        var deletedUser = await _context.Users.FindAsync(user.Id);
        Assert.IsNull(deletedUser);
    }

    [TestMethod]
    public async Task CreateSessionAsync_WithValidData_CreatesSession()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.CreateSessionAsync(
            user.Id, 
            "session-token", 
            "192.168.1.1", 
            "Test User Agent",
            SessionType.Web);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(user.Id, result.UserId);
        Assert.AreEqual("session-token", result.SessionToken);
        Assert.AreEqual("192.168.1.1", result.IpAddress);
        Assert.AreEqual("Test User Agent", result.UserAgent);
        Assert.AreEqual(SessionType.Web, result.SessionType);
        Assert.IsTrue(result.IsActive);
    }

    [TestMethod]
    public async Task ValidateSessionAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password"
        };
        _context.Users.Add(user);

        var session = new AppUserSession
        {
            UserId = user.Id,
            SessionToken = "valid-token",
            State = Diiwo.Core.Domain.Enums.EntityState.Active,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.ValidateSessionAsync("valid-token");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ValidateSessionAsync_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password"
        };
        _context.Users.Add(user);

        var session = new AppUserSession
        {
            UserId = user.Id,
            SessionToken = "expired-token",
            State = Diiwo.Core.Domain.Enums.EntityState.Active,
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };
        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.ValidateSessionAsync("expired-token");

        // Assert
        Assert.IsFalse(result);
    }
}