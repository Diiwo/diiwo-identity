using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Diiwo.Identity.App.DbContext;
using Diiwo.Identity.App.Application.Services;
using Diiwo.Identity.App.Entities;
using Diiwo.Identity.Shared.Enums;

namespace App.Tests.Services;

/// <summary>
/// Test suite for AppPermissionService
/// Validates permission management operations, authorization logic, and business rules
/// </summary>
[TestClass]
public class AppPermissionServiceTests
{
    private AppIdentityDbContext _context = null!;
    private AppPermissionService _permissionService = null!;
    private Mock<ILogger<AppPermissionService>> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppIdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppIdentityDbContext(options);
        _mockLogger = new Mock<ILogger<AppPermissionService>>();
        _permissionService = new AppPermissionService(_context, _mockLogger.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    /// <summary>
    /// Test Case: Direct User Permission Authorization
    /// Description: Verifies that users with direct permissions are correctly authorized
    /// Acceptance Criteria:
    /// - User should be granted access when they have direct permission
    /// - Permission check should return true for granted permissions
    /// - Permission scope and priority should be respected
    /// </summary>
    [TestMethod]
    public async Task UserHasPermissionAsync_WithDirectUserPermission_ReturnsTrue()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password"
        };
        _context.Users.Add(user);

        var permission = new AppPermission
        {
            Resource = "Document",
            Action = "Read",
            Scope = PermissionScope.Global
        };
        _context.Permissions.Add(permission);

        var userPermission = new AppUserPermission
        {
            UserId = user.Id,
            PermissionId = permission.Id,
            IsGranted = true,
            Priority = 100
        };
        _context.UserPermissions.Add(userPermission);

        await _context.SaveChangesAsync();

        // Act
        var result = await _permissionService.UserHasPermissionAsync(user.Id, "Document", "Read");

        // Assert
        Assert.IsTrue(result, "User should have permission when directly granted");
    }

    /// <summary>
    /// Test Case: Unauthorized Access Prevention
    /// Description: Verifies that users without permissions are correctly denied access
    /// Acceptance Criteria:
    /// - User should be denied access when they lack required permissions
    /// - Permission check should return false for unauthorized requests
    /// - System should handle non-existent permissions gracefully
    /// </summary>
    [TestMethod]
    public async Task UserHasPermissionAsync_WithoutPermission_ReturnsFalse()
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
        var result = await _permissionService.UserHasPermissionAsync(user.Id, "Document", "Read");

        // Assert
        Assert.IsFalse(result, "User should not have permission when not granted");
    }

    /// <summary>
    /// Test Case: Group-based Permission Authorization
    /// Description: Verifies that users inherit permissions from their group memberships
    /// Acceptance Criteria:
    /// - User should inherit permissions from groups they belong to
    /// - Group permission priorities should be respected
    /// - Permission inheritance should work correctly across group hierarchies
    /// </summary>
    [TestMethod]
    public async Task UserHasPermissionAsync_WithGroupPermission_ReturnsTrue()
    {
        // Arrange
        var group = new AppGroup
        {
            Name = "Admin",
            Description = "Administrator group"
        };
        _context.Groups.Add(group);

        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password"
        };
        user.UserGroups.Add(group); // Add user to group
        _context.Users.Add(user);

        var permission = new AppPermission
        {
            Resource = "Admin",
            Action = "Access",
            Scope = PermissionScope.Global
        };
        _context.Permissions.Add(permission);

        var groupPermission = new AppGroupPermission
        {
            GroupId = group.Id,
            PermissionId = permission.Id,
            IsGranted = true,
            Priority = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.GroupPermissions.Add(groupPermission);

        await _context.SaveChangesAsync();

        // Act
        var result = await _permissionService.UserHasPermissionAsync(user.Id, "Admin", "Access");

        // Assert
        Assert.IsTrue(result, "User should inherit permission from group membership");
    }

    [TestMethod]
    public async Task GrantUserPermissionAsync_WithValidData_GrantsPermission()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password"
        };
        _context.Users.Add(user);

        var permission = new AppPermission
        {
            Resource = "Document",
            Action = "Write",
            Scope = PermissionScope.Global
        };
        _context.Permissions.Add(permission);

        await _context.SaveChangesAsync();

        // Act
        var result = await _permissionService.GrantUserPermissionAsync(
            user.Id, 
            "Document", 
            "Write", 
            expiresAt: null, 
            priority: 100);

        // Assert
        Assert.IsTrue(result);

        var userPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == user.Id && up.PermissionId == permission.Id);
        
        Assert.IsNotNull(userPermission);
        Assert.IsTrue(userPermission.IsGranted);
        Assert.AreEqual(100, userPermission.Priority);
    }

    [TestMethod]
    public async Task RevokeUserPermissionAsync_WithExistingPermission_RevokesPermission()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password"
        };
        _context.Users.Add(user);

        var permission = new AppPermission
        {
            Resource = "Document",
            Action = "Delete",
            Scope = PermissionScope.Global
        };
        _context.Permissions.Add(permission);

        var userPermission = new AppUserPermission
        {
            UserId = user.Id,
            PermissionId = permission.Id,
            IsGranted = true,
            Priority = 100
        };
        _context.UserPermissions.Add(userPermission);

        await _context.SaveChangesAsync();

        // Act
        var result = await _permissionService.RevokeUserPermissionAsync(user.Id, "Document", "Delete");

        // Assert
        Assert.IsTrue(result);

        var revokedPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == user.Id && up.PermissionId == permission.Id);
        
        Assert.IsNull(revokedPermission);
    }

    [TestMethod]
    public async Task UserHasPermissionsAsync_WithMultiplePermissions_ReturnsCorrectResults()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password"
        };
        _context.Users.Add(user);

        var readPermission = new AppPermission
        {
            Resource = "Document",
            Action = "Read",
            Scope = PermissionScope.Global
        };
        var writePermission = new AppPermission
        {
            Resource = "Document",
            Action = "Write",
            Scope = PermissionScope.Global
        };
        _context.Permissions.AddRange(readPermission, writePermission);

        var userReadPermission = new AppUserPermission
        {
            UserId = user.Id,
            PermissionId = readPermission.Id,
            IsGranted = true,
            Priority = 100
        };
        _context.UserPermissions.Add(userReadPermission);

        await _context.SaveChangesAsync();

        // Act
        var result = await _permissionService.UserHasPermissionsAsync(user.Id, "Document.Read", "Document.Write");

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result["Document.Read"]);
        Assert.IsFalse(result["Document.Write"]);
    }
}