using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Diiwo.Identity.App.DbContext;
using Diiwo.Identity.App.Application.Services;
using Diiwo.Identity.App.Entities;
using Diiwo.Identity.Shared.Enums;

namespace App.Tests.Services;

/// <summary>
/// Tests for AppUserService
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
        Assert.IsNotNull(result);
        Assert.AreEqual(user.Id, result.Id);
        Assert.AreEqual("test@example.com", result.Email);
    }

    [TestMethod]
    public async Task GetUserByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _userService.GetUserByIdAsync(Guid.NewGuid());

        // Assert
        Assert.IsNull(result);
    }

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
        Assert.IsNotNull(result);
        Assert.AreEqual("test@example.com", result.Email);
    }

    [TestMethod]
    public async Task GetUserByEmailAsync_WithInvalidEmail_ReturnsNull()
    {
        // Act
        var result = await _userService.GetUserByEmailAsync("notfound@example.com");

        // Assert
        Assert.IsNull(result);
    }

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
        Assert.IsNotNull(result);
        Assert.AreEqual("newuser@example.com", result.Email);
        Assert.AreEqual("John", result.FirstName);
        Assert.AreEqual("Doe", result.LastName);
        Assert.IsTrue(result.IsActive);

        var userInDb = await _context.Users.FindAsync(result.Id);
        Assert.IsNotNull(userInDb);
    }

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
            IsActive = true,
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
            IsActive = true,
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