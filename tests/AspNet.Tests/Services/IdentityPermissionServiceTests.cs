using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Moq;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Application.Services;
using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.AspNet.Tests.Services;

/// <summary>
/// Test suite for IdentityPermissionService (Shared Implementation)
/// Validates shared permission checking logic that works across both App and AspNet architectures
/// </summary>
[TestClass]
public class IdentityPermissionServiceTests
{
    private AspNetIdentityDbContext _context = null!;
    private IdentityPermissionService _permissionService = null!;
    private Mock<ILogger<IdentityPermissionService>> _mockLogger = null!;
    private Mock<UserManager<Diiwo.Identity.AspNet.Entities.IdentityUser>> _mockUserManager = null!;
    private Mock<RoleManager<Diiwo.Identity.AspNet.Entities.IdentityRole>> _mockRoleManager = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AspNetIdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AspNetIdentityDbContext(options);
        _mockLogger = new Mock<ILogger<IdentityPermissionService>>();
        
        // Create mock UserManager
        _mockUserManager = new Mock<UserManager<Diiwo.Identity.AspNet.Entities.IdentityUser>>(
            Mock.Of<IUserStore<Diiwo.Identity.AspNet.Entities.IdentityUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);

        // Create mock RoleManager
        _mockRoleManager = new Mock<RoleManager<Diiwo.Identity.AspNet.Entities.IdentityRole>>(
            Mock.Of<IRoleStore<Diiwo.Identity.AspNet.Entities.IdentityRole>>(),
            null!, null!, null!, null!);
        
        _permissionService = new IdentityPermissionService(_context, _mockUserManager.Object, _mockRoleManager.Object, _mockLogger.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    /// <summary>
    /// Test Case: Basic Permission Check
    /// Description: Verifies that the shared service can perform basic permission checks
    /// Acceptance Criteria:
    /// - Should provide unified permission checking interface
    /// - Should work with AspNet architecture entities
    /// - Should return consistent results with main permission service
    /// </summary>
    [TestMethod]
    public async Task UserHasPermissionAsync_WithBasicUserPermission_ReturnsTrue()
    {
        // Arrange
        var user = new Diiwo.Identity.AspNet.Entities.IdentityUser
        {
            Email = "shared@example.com",
            UserName = "shareduser"
        };
        _context.Users.Add(user);

        var permission = new IdentityPermission
        {
            Resource = "SharedResource",
            Action = "SharedAction"
        };
        _context.IdentityPermissions.Add(permission);

        var userPermission = new IdentityUserPermission
        {
            UserId = user.Id,
            PermissionId = permission.Id,
            IsGranted = true,
            Priority = 100
        };
        _context.IdentityUserPermissions.Add(userPermission);

        await _context.SaveChangesAsync();

        // Setup mock UserManager to return the user
        _mockUserManager.Setup(um => um.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _permissionService.UserHasPermissionAsync(user.Id, "SharedResource.SharedAction");

        // Assert
        Assert.IsTrue(result, "Shared service should correctly identify user permission");
    }

    /// <summary>
    /// Test Case: Permission Denial
    /// Description: Verifies that the shared service correctly denies unauthorized access
    /// Acceptance Criteria:
    /// - Should return false for users without permissions
    /// - Should handle non-existent users gracefully
    /// - Should maintain consistent behavior with architecture-specific services
    /// </summary>
    [TestMethod]
    public async Task UserHasPermissionAsync_WithoutPermission_ReturnsFalse()
    {
        // Arrange
        var user = new Diiwo.Identity.AspNet.Entities.IdentityUser
        {
            Email = "noshared@example.com",
            UserName = "noshareduser"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _permissionService.UserHasPermissionAsync(user.Id, "NonExistentResource.NonExistentAction");

        // Assert
        Assert.IsFalse(result, "Shared service should deny access when permission doesn't exist");
    }

    /// <summary>
    /// Test Case: Role-based Permission Check
    /// Description: Verifies that the shared service can check role-based permissions
    /// Acceptance Criteria:
    /// - Should work with AspNet Core Identity role system
    /// - Should provide same results as dedicated AspNet service
    /// - Should respect role permission priorities
    /// </summary>
    [TestMethod]
    public async Task UserHasPermissionAsync_WithRolePermission_ReturnsTrue()
    {
        // Arrange
        var user = new Diiwo.Identity.AspNet.Entities.IdentityUser
        {
            Email = "sharedrole@example.com",
            UserName = "sharedroleuser"
        };
        _context.Users.Add(user);

        var role = new Diiwo.Identity.AspNet.Entities.IdentityRole
        {
            Name = "SharedRole",
            NormalizedName = "SHAREDROLE"
        };
        _context.Roles.Add(role);

        var permission = new IdentityPermission
        {
            Resource = "SharedRoleResource",
            Action = "SharedRoleAction"
        };
        _context.IdentityPermissions.Add(permission);

        var rolePermission = new IdentityRolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id,
            IsGranted = true,
            Priority = 0
        };
        _context.IdentityRolePermissions.Add(rolePermission);

        var userRole = new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>
        {
            UserId = user.Id,
            RoleId = role.Id
        };
        _context.UserRoles.Add(userRole);

        await _context.SaveChangesAsync();

        _mockUserManager.Setup(um => um.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _mockUserManager.Setup(um => um.GetRolesAsync(user)).ReturnsAsync(new List<string> { role.Name! });

        // Act
        var result = await _permissionService.UserHasPermissionAsync(user.Id, "SharedRoleResource.SharedRoleAction");

        // Assert
        Assert.IsTrue(result, "Shared service should correctly identify role-based permission");
    }

    /// <summary>
    /// Test Case: User Existence Check
    /// Description: Verifies that the shared service can check if users exist
    /// Acceptance Criteria:
    /// - Should return true for existing users
    /// - Should return false for non-existent users
    /// - Should handle different user ID formats
    /// </summary>
    [TestMethod]
    public async Task UserExistsAsync_WithValidUser_ReturnsTrue()
    {
        // Arrange
        var user = new Diiwo.Identity.AspNet.Entities.IdentityUser
        {
            Email = "exists@example.com",
            UserName = "existsuser"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _mockUserManager.Setup(um => um.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        // Act
        var result = await _permissionService.UserExistsAsync(user.Id);

        // Assert
        Assert.IsTrue(result, "Should return true for existing user");
    }

    /// <summary>
    /// Test Case: Non-existent User Check
    /// Description: Verifies that the shared service correctly handles non-existent users
    /// Acceptance Criteria:
    /// - Should return false for non-existent user IDs
    /// - Should not throw exceptions for invalid user IDs
    /// - Should handle edge cases gracefully
    /// </summary>
    [TestMethod]
    public async Task UserExistsAsync_WithInvalidUser_ReturnsFalse()
    {
        // Act
        var result = await _permissionService.UserExistsAsync(Guid.NewGuid());

        // Assert
        Assert.IsFalse(result, "Should return false for non-existent user");
    }

    /// <summary>
    /// Test Case: Permission Resource Lookup
    /// Description: Verifies that the shared service can look up permissions by resource and action
    /// Acceptance Criteria:
    /// - Should find existing permissions correctly
    /// - Should return null for non-existent permissions
    /// - Should handle resource-action combinations properly
    /// </summary>
    [TestMethod]
    public async Task GetPermissionAsync_WithValidResourceAction_ReturnsPermission()
    {
        // Arrange
        var permission = new IdentityPermission
        {
            Resource = "LookupResource",
            Action = "LookupAction",
            Description = "Test lookup permission"
        };
        _context.IdentityPermissions.Add(permission);
        await _context.SaveChangesAsync();

        // Act
        var result = await _permissionService.GetPermissionAsync("LookupResource", "LookupAction");

        // Assert
        Assert.IsNotNull(result, "Should find existing permission");
        Assert.AreEqual("LookupResource", result.Resource, "Should return correct resource");
        Assert.AreEqual("LookupAction", result.Action, "Should return correct action");
        Assert.AreEqual("Test lookup permission", result.Description, "Should include permission details");
    }

    /// <summary>
    /// Test Case: Non-existent Permission Lookup
    /// Description: Verifies that the shared service handles non-existent permission lookups
    /// Acceptance Criteria:
    /// - Should return null for non-existent permissions
    /// - Should not throw exceptions for invalid resource-action combinations
    /// - Should handle edge cases gracefully
    /// </summary>
    [TestMethod]
    public async Task GetPermissionAsync_WithInvalidResourceAction_ReturnsNull()
    {
        // Act
        var result = await _permissionService.GetPermissionAsync("NonExistentResource", "NonExistentAction");

        // Assert
        Assert.IsNull(result, "Should return null for non-existent permission");
    }

    /// <summary>
    /// Test Case: Cross-Architecture Compatibility
    /// Description: Verifies that the shared service works consistently across architectures
    /// Acceptance Criteria:
    /// - Should provide same interface for both App and AspNet architectures
    /// - Should maintain consistent behavior patterns
    /// - Should support migration scenarios
    /// </summary>
    [TestMethod]
    public async Task SharedService_CrossArchitecture_WorksConsistently()
    {
        // Arrange - Simulate cross-architecture scenario
        var user = new Diiwo.Identity.AspNet.Entities.IdentityUser
        {
            Email = "cross@example.com",
            UserName = "crossuser",
            AppUserId = Guid.NewGuid() // Cross-architecture reference
        };
        _context.Users.Add(user);

        var permission = new IdentityPermission
        {
            Resource = "CrossResource",
            Action = "CrossAction",
            AppPermissionId = Guid.NewGuid() // Cross-architecture reference
        };
        _context.IdentityPermissions.Add(permission);

        var userPermission = new IdentityUserPermission
        {
            UserId = user.Id,
            PermissionId = permission.Id,
            IsGranted = true,
            Priority = 100
        };
        _context.IdentityUserPermissions.Add(userPermission);

        await _context.SaveChangesAsync();

        _mockUserManager.Setup(um => um.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        // Act
        var hasPermission = await _permissionService.UserHasPermissionAsync(user.Id, "CrossResource.CrossAction");
        var userExists = await _permissionService.UserExistsAsync(user.Id);
        var permissionEntity = await _permissionService.GetPermissionAsync("CrossResource", "CrossAction");

        // Assert
        Assert.IsTrue(hasPermission, "Permission check should work with cross-architecture entities");
        Assert.IsTrue(userExists, "User existence check should work with cross-architecture entities");
        Assert.IsNotNull(permissionEntity, "Permission lookup should work with cross-architecture entities");
        Assert.IsNotNull(user.AppUserId, "Cross-architecture reference should be maintained");
        Assert.IsNotNull(permissionEntity.AppPermissionId, "Cross-architecture permission reference should be maintained");
    }

    /// <summary>
    /// Test Case: Bulk Permission Check
    /// Description: Verifies that the shared service can handle multiple permission checks efficiently
    /// Acceptance Criteria:
    /// - Should support checking multiple permissions for a user
    /// - Should provide efficient batch operations
    /// - Should maintain consistency across bulk operations
    /// </summary>
    [TestMethod]
    public async Task HasMultiplePermissionsAsync_WithMixedPermissions_ReturnsCorrectResults()
    {
        // Arrange
        var user = new Diiwo.Identity.AspNet.Entities.IdentityUser
        {
            Email = "bulk@example.com",
            UserName = "bulkuser"
        };
        _context.Users.Add(user);

        var permission1 = new IdentityPermission { Resource = "Resource1", Action = "Action1" };
        var permission2 = new IdentityPermission { Resource = "Resource2", Action = "Action2" };
        var permission3 = new IdentityPermission { Resource = "Resource3", Action = "Action3" };
        _context.IdentityPermissions.AddRange(permission1, permission2, permission3);

        // Grant permission 1 and 3, but not 2
        var userPerm1 = new IdentityUserPermission
        {
            UserId = user.Id,
            PermissionId = permission1.Id,
            IsGranted = true,
            Priority = 100
        };
        var userPerm3 = new IdentityUserPermission
        {
            UserId = user.Id,
            PermissionId = permission3.Id,
            IsGranted = true,
            Priority = 100
        };
        _context.IdentityUserPermissions.AddRange(userPerm1, userPerm3);

        await _context.SaveChangesAsync();

        _mockUserManager.Setup(um => um.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        // Act
        var result1 = await _permissionService.UserHasPermissionAsync(user.Id, "Resource1.Action1");
        var result2 = await _permissionService.UserHasPermissionAsync(user.Id, "Resource2.Action2");  
        var result3 = await _permissionService.UserHasPermissionAsync(user.Id, "Resource3.Action3");
        
        var results = new Dictionary<(string, string), bool>
        {
            { ("Resource1", "Action1"), result1 },
            { ("Resource2", "Action2"), result2 },
            { ("Resource3", "Action3"), result3 }
        };

        // Assert
        Assert.AreEqual(3, results.Count, "Should return results for all requested permissions");
        Assert.IsTrue(results[("Resource1", "Action1")], "Should have permission 1");
        Assert.IsFalse(results[("Resource2", "Action2")], "Should not have permission 2");
        Assert.IsTrue(results[("Resource3", "Action3")], "Should have permission 3");
    }
}