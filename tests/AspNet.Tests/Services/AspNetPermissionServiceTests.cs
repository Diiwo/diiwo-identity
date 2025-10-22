using Diiwo.Identity.AspNet.Application.Services;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.AspNet.Tests.Utilities.Factories;
using Diiwo.Identity.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using IdentityRole = Diiwo.Identity.AspNet.Entities.IdentityRole;
using IdentityUser = Diiwo.Identity.AspNet.Entities.IdentityUser;

namespace Diiwo.Identity.AspNet.Tests.Services;

/// <summary>
/// Test suite for AspNetPermissionService
/// Validates enterprise permission management operations, 5-level authorization logic, and business rules
/// </summary>
[TestClass]
public class AspNetPermissionServiceTests
{
    private AspNetIdentityDbContext _context = null!;
    private AspNetPermissionService _permissionService = null!;
    private Mock<ILogger<AspNetPermissionService>> _mockLogger = null!;
    private UserManager<IdentityUser> _userManager = null!;
    private RoleManager<IdentityRole> _roleManager = null!;

    [TestInitialize]
    public void Setup()
    {
        // Setup DbContext with InMemory database
        var options = new DbContextOptionsBuilder<AspNetIdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AspNetIdentityDbContext(options);

        // Setup mocks
        _mockLogger = new Mock<ILogger<AspNetPermissionService>>();
        _userManager = IdentityTestFactory.CreateUserManager(_context);
        _roleManager = IdentityTestFactory.CreateRoleManager(_context);

        // Initialize service
        _permissionService = new AspNetPermissionService(
            _context,
            _userManager,
            _roleManager, 
            _mockLogger.Object);
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
    /// - Should respect permission scope and priority in 5-level system
    /// </summary>
    [TestMethod]
    public async Task UserHasPermissionAsync_WithDirectUserPermission_ReturnsTrue()
    {
        // Arrange
        var user = new IdentityUser
        {
            Email = "direct@example.com",
            UserName = "directuser"
        };
        _context.Users.Add(user);

        var permission = new IdentityPermission
        {
            Resource = "Documents",
            Action = "Read",
            Scope = PermissionScope.Global
        };
        _context.IdentityPermissions.Add(permission);

        await _context.SaveChangesAsync(); // Save first so IDs are generated

        var userPermission = new IdentityUserPermission
        {
            UserId = user.Id,
            PermissionId = permission.Id,
            IsGranted = true,
            Priority = 100  // User level priority
        };
        _context.IdentityUserPermissions.Add(userPermission);

        await _context.SaveChangesAsync();

        // Debug: Verify the permission was created
        var createdPermission = await _context.IdentityPermissions
            .FirstOrDefaultAsync(p => p.Resource == "Documents" && p.Action == "Read");
        Assert.IsNotNull(createdPermission, "Permission should exist in database");

        // Debug: Verify the user permission was created
        var createdUserPermission = await _context.IdentityUserPermissions
            .Include(up => up.Permission)
            .FirstOrDefaultAsync(up => up.UserId == user.Id && up.PermissionId == permission.Id);
        Assert.IsNotNull(createdUserPermission, "User permission should exist in database");
        Assert.IsTrue(createdUserPermission.IsGranted, "User permission should be granted");

        // Debug: Check if permission lookup is working
        var foundPermission = await _context.IdentityPermissions
            .FirstOrDefaultAsync(p => p.Resource == "Documents" && p.Action == "Read" && p.State == Diiwo.Core.Domain.Enums.EntityState.Active);
        Assert.IsNotNull(foundPermission, "Permission should be found with State filter");

        // Debug: Check direct query for user permission with all filters
        var directUserPermission = await _context.IdentityUserPermissions
            .Include(up => up.Permission)
            .FirstOrDefaultAsync(up => up.UserId == user.Id &&
                                     up.Permission.Resource == "Documents" &&
                                     up.Permission.Action == "Read" &&
                                     up.Permission.State == Diiwo.Core.Domain.Enums.EntityState.Active);
        Assert.IsNotNull(directUserPermission, "Direct user permission query should find the permission");

        // Act
        var result = await _permissionService.UserHasPermissionAsync(user.Id, "Documents", "Read");

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
        var user = new IdentityUser
        {
            Email = "noperm@example.com",
            UserName = "nopermuser"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _permissionService.UserHasPermissionAsync(user.Id, "Documents", "Read");

        // Assert
        Assert.IsFalse(result, "User should not have permission when not granted");
    }

    /// <summary>
    /// Test Case: Role-based Permission Authorization (Level 1)
    /// Description: Verifies that users inherit permissions from their roles
    /// Acceptance Criteria:
    /// - User should inherit permissions from assigned roles
    /// - Role permissions should have highest priority (0)
    /// - Should work with AspNet Core Identity role system
    /// </summary>
    [TestMethod]
    public async Task UserHasPermissionAsync_WithRolePermission_ReturnsTrue()
    {
        // Arrange
        var user = new IdentityUser
        {
            Email = "role@example.com",
            UserName = "roleuser"
        };
        _context.Users.Add(user);

        var role = new IdentityRole
        {
            Name = "Administrator",
            NormalizedName = "ADMINISTRATOR"
        };
        _context.Roles.Add(role);

        var permission = new IdentityPermission
        {
            Resource = "System",
            Action = "Admin"
        };
        _context.IdentityPermissions.Add(permission);

        // Create role permission
        var rolePermission = new IdentityRolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id,
            IsGranted = true,
            Priority = 0  // Highest priority
        };
        _context.IdentityRolePermissions.Add(rolePermission);

        // Assign user to role
        var userRole = new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>
        {
            UserId = user.Id,
            RoleId = role.Id
        };
        _context.UserRoles.Add(userRole);

        await _context.SaveChangesAsync();

        // Act
        var result = await _permissionService.UserHasPermissionAsync(user.Id, "System", "Admin");

        // Assert
        Assert.IsTrue(result, "User should inherit permission from role membership");
    }

    /// <summary>
    /// Test Case: Group-based Permission Authorization (Level 2)
    /// Description: Verifies that users inherit permissions from their group memberships
    /// Acceptance Criteria:
    /// - User should inherit permissions from groups they belong to
    /// - Group permission priorities should be respected (50)
    /// - Should work with custom group system
    /// </summary>
    [TestMethod]
    public async Task UserHasPermissionAsync_WithGroupPermission_ReturnsTrue()
    {
        // Arrange
        var user = new IdentityUser
        {
            Email = "group@example.com",
            UserName = "groupuser"
        };
        _context.Users.Add(user);

        var group = new IdentityGroup
        {
            Name = "Editors",
            Description = "Content editors group"
        };
        _context.IdentityGroups.Add(group);

        var permission = new IdentityPermission
        {
            Resource = "Content",
            Action = "Edit"
        };
        _context.IdentityPermissions.Add(permission);

        // Create group permission
        var groupPermission = new IdentityGroupPermission
        {
            GroupId = group.Id,
            PermissionId = permission.Id,
            IsGranted = true,
            Priority = 50  // Group level priority
        };
        _context.IdentityGroupPermissions.Add(groupPermission);

        // Add user to group
        user.IdentityUserGroups.Add(group);
        group.Users.Add(user);

        await _context.SaveChangesAsync();

        // Act
        var result = await _permissionService.UserHasPermissionAsync(user.Id, "Content", "Edit");

        // Assert
        Assert.IsTrue(result, "User should inherit permission from group membership");
    }

    /// <summary>
    /// Test Case: Permission Priority System (Deny Wins)
    /// Description: Verifies that DENY permissions always override GRANT permissions
    /// Acceptance Criteria:
    /// - DENY should always win over GRANT regardless of priority
    /// - Should test multiple permission levels with conflicting grants/denies
    /// - Should follow enterprise security principle of "deny by default"
    /// </summary>
    [TestMethod]
    public async Task UserHasPermissionAsync_WithDenyPermission_ReturnsFalse()
    {
        // Arrange
        var user = new IdentityUser
        {
            Email = "deny@example.com",
            UserName = "denyuser"
        };
        _context.Users.Add(user);

        var role = new IdentityRole
        {
            Name = "BasicUser",
            NormalizedName = "BASICUSER"
        };
        _context.Roles.Add(role);

        var permission = new IdentityPermission
        {
            Resource = "Sensitive",
            Action = "Access"
        };
        _context.IdentityPermissions.Add(permission);

        // Grant permission at role level (higher priority)
        var rolePermission = new IdentityRolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id,
            IsGranted = true,
            Priority = 0
        };
        _context.IdentityRolePermissions.Add(rolePermission);

        // Deny permission at user level (lower priority, but DENY wins)
        var userPermission = new IdentityUserPermission
        {
            UserId = user.Id,
            PermissionId = permission.Id,
            IsGranted = false,  // DENY
            Priority = 100
        };
        _context.IdentityUserPermissions.Add(userPermission);

        // Assign user to role
        var userRole = new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>
        {
            UserId = user.Id,
            RoleId = role.Id
        };
        _context.UserRoles.Add(userRole);

        await _context.SaveChangesAsync();

        // Act
        var result = await _permissionService.UserHasPermissionAsync(user.Id, "Sensitive", "Access");

        // Assert
        Assert.IsFalse(result, "DENY permission should override GRANT permission regardless of priority");
    }

    /// <summary>
    /// Test Case: Permission Creation and Management
    /// Description: Verifies that permissions can be created and managed correctly
    /// Acceptance Criteria:
    /// - Should create permissions with proper validation
    /// - Should prevent duplicate permissions for same resource-action combination
    /// - Should support different permission scopes
    /// </summary>
    [TestMethod]
    public async Task CreatePermissionAsync_WithValidData_CreatesPermission()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();

        // Act
        var result = await _permissionService.CreatePermissionAsync(
            "Reports", 
            "Generate", 
            "Permission to generate reports", 
            PermissionScope.Model);

        // Assert
        Assert.IsNotNull(result, "Permission should be created successfully");
        Assert.AreEqual("Reports", result.Resource, "Resource should be set correctly");
        Assert.AreEqual("Generate", result.Action, "Action should be set correctly");
        Assert.AreEqual("Permission to generate reports", result.Description, "Description should be set");
        Assert.AreEqual(PermissionScope.Model, result.Scope, "Scope should be set correctly");

        // Verify in database
        var permissionInDb = await _context.IdentityPermissions
            .FirstOrDefaultAsync(p => p.Resource == "Reports" && p.Action == "Generate");
        Assert.IsNotNull(permissionInDb, "Permission should be persisted to database");
    }

    /// <summary>
    /// Test Case: User Permission Grant Operations
    /// Description: Verifies that permissions can be granted to users correctly
    /// Acceptance Criteria:
    /// - Should grant permissions to users with proper audit trail
    /// - Should support different priority levels
    /// - Should handle both grant and deny operations
    /// </summary>
    [TestMethod]
    public async Task GrantUserPermissionAsync_WithValidData_GrantsPermission()
    {
        // Arrange
        var user = new IdentityUser
        {
            Email = "grant@example.com",
            UserName = "grantuser"
        };
        _context.Users.Add(user);

        var permission = new IdentityPermission
        {
            Resource = "Files",
            Action = "Upload"
        };
        _context.IdentityPermissions.Add(permission);
        await _context.SaveChangesAsync();

        // Act
        var result = await _permissionService.AssignPermissionToUserAsync(
            user.Id,
            permission.Resource,
            permission.Action,
            true,  // Grant permission
            100);  // User level priority

        // Assert
        Assert.IsTrue(result, "Permission should be granted successfully");

        // Verify user now has permission
        var hasPermission = await _permissionService.UserHasPermissionAsync(
            user.Id, "Files", "Upload");
        Assert.IsTrue(hasPermission, "User should have the granted permission");
    }
}