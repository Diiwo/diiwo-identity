using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.AspNet.Tests.Integration;

/// <summary>
/// Integration test suite for AspNetIdentityDbContext
/// Validates database operations, entity relationships, and enterprise data persistence
/// </summary>
[TestClass]
public class AspNetIdentityDbContextTests
{
    private AspNetIdentityDbContext _context = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AspNetIdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AspNetIdentityDbContext(options);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    /// <summary>
    /// Test Case: Database Context Initialization
    /// Description: Verifies that AspNetIdentityDbContext initializes correctly with all DbSets
    /// Acceptance Criteria:
    /// - All required DbSets should be available
    /// - Context should inherit from IdentityDbContext properly
    /// - Should support both AspNet and cross-architecture entities
    /// </summary>
    [TestMethod]
    public void AspNetDbContext_Initialization_ConfiguresAllDbSets()
    {
        // Assert - Core Identity entities
        Assert.IsNotNull(_context.Users, "Users DbSet should be configured");
        Assert.IsNotNull(_context.Roles, "Roles DbSet should be configured");
        Assert.IsNotNull(_context.UserRoles, "UserRoles DbSet should be configured");
        Assert.IsNotNull(_context.UserClaims, "UserClaims DbSet should be configured");
        Assert.IsNotNull(_context.UserLogins, "UserLogins DbSet should be configured");
        Assert.IsNotNull(_context.UserTokens, "UserTokens DbSet should be configured");
        Assert.IsNotNull(_context.RoleClaims, "RoleClaims DbSet should be configured");

        // Assert - Custom AspNet entities
        Assert.IsNotNull(_context.IdentityGroups, "Groups DbSet should be configured");
        Assert.IsNotNull(_context.IdentityPermissions, "Permissions DbSet should be configured");
        Assert.IsNotNull(_context.IdentityUserSessions, "UserSessions DbSet should be configured");
        Assert.IsNotNull(_context.IdentityLoginHistory, "IdentityLoginHistory DbSet should be configured");
        
        // Assert - 5-level permission system entities
        Assert.IsNotNull(_context.IdentityRolePermissions, "RolePermissions DbSet should be configured");
        Assert.IsNotNull(_context.IdentityGroupPermissions, "GroupPermissions DbSet should be configured");
        Assert.IsNotNull(_context.IdentityUserPermissions, "UserPermissions DbSet should be configured");
        Assert.IsNotNull(_context.IdentityModelPermissions, "IdentityModelPermissions DbSet should be configured");
        Assert.IsNotNull(_context.IdentityObjectPermissions, "IdentityObjectPermissions DbSet should be configured");
    }

    /// <summary>
    /// Test Case: User Entity CRUD Operations
    /// Description: Verifies that IdentityUser entities can be created, read, updated, and deleted
    /// Acceptance Criteria:
    /// - Should support full CRUD operations
    /// - Should maintain AspNet Core Identity properties
    /// - Should handle custom enterprise properties correctly
    /// </summary>
    [TestMethod]
    public async Task IdentityUser_CrudOperations_WorkCorrectly()
    {
        // Create
        var user = new IdentityUser
        {
            Email = "crud@example.com",
            UserName = "cruduser",
            FirstName = "CRUD",
            LastName = "User",
            PhoneNumber = "+1234567890"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Read
        var retrievedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "crud@example.com");

        Assert.IsNotNull(retrievedUser, "User should be successfully created and retrieved");
        Assert.AreEqual("crud@example.com", retrievedUser.Email, "Email should be persisted correctly");
        Assert.AreEqual("CRUD User", retrievedUser.FullName, "Full name should be computed correctly");

        // Update
        retrievedUser.FirstName = "Updated";
        retrievedUser.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var updatedUser = await _context.Users.FindAsync(retrievedUser.Id);
        Assert.AreEqual("Updated", updatedUser!.FirstName, "User should be updated successfully");

        // Delete
        _context.Users.Remove(updatedUser);
        await _context.SaveChangesAsync();

        var deletedUser = await _context.Users.FindAsync(updatedUser.Id);
        Assert.IsNull(deletedUser, "User should be deleted successfully");
    }

    /// <summary>
    /// Test Case: Permission Entity Management
    /// Description: Verifies that IdentityPermission entities are managed correctly
    /// Acceptance Criteria:
    /// - Should support creating permissions with different scopes
    /// - Should maintain permission uniqueness by resource-action combination
    /// - Should support permission lookup operations
    /// </summary>
    [TestMethod]
    public async Task IdentityPermission_Management_WorksCorrectly()
    {
        // Arrange & Act - Create permissions
        var readPermission = new IdentityPermission
        {
            Resource = "Documents",
            Action = "Read",
            Description = "Read document permission",
            Scope = PermissionScope.Global
        };

        var writePermission = new IdentityPermission
        {
            Resource = "Documents",
            Action = "Write",
            Description = "Write document permission",
            Scope = PermissionScope.Model
        };

        _context.IdentityPermissions.AddRange(readPermission, writePermission);
        await _context.SaveChangesAsync();

        // Assert - Verify permissions are stored
        var permissions = await _context.IdentityPermissions
            .Where(p => p.Resource == "Documents")
            .ToListAsync();

        Assert.AreEqual(2, permissions.Count, "Both permissions should be created");
        Assert.IsTrue(permissions.Any(p => p.Action == "Read"), "Read permission should exist");
        Assert.IsTrue(permissions.Any(p => p.Action == "Write"), "Write permission should exist");

        // Assert - Verify permission keys are unique
        var readPerm = permissions.First(p => p.Action == "Read");
        var writePerm = permissions.First(p => p.Action == "Write");
        
        Assert.AreEqual("Documents.Read", readPerm.Name, "Read permission should have correct key");
        Assert.AreEqual("Documents.Write", writePerm.Name, "Write permission should have correct key");
        Assert.AreNotEqual(readPerm.Name, writePerm.Name, "Permission keys should be unique");
    }

    /// <summary>
    /// Test Case: User Session Management
    /// Description: Verifies that user sessions are properly managed and related to users
    /// Acceptance Criteria:
    /// - Should create sessions linked to users
    /// - Should support different session types
    /// - Should maintain session security information
    /// </summary>
    [TestMethod]
    public async Task UserSession_Management_WorksCorrectly()
    {
        // Arrange - Create user first
        var user = new IdentityUser
        {
            Email = "session@example.com",
            UserName = "sessionuser"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act - Create sessions
        var webSession = new IdentityUserSession
        {
            UserId = user.Id,
            SessionToken = "web-session-token",
            SessionType = SessionType.Web,
            IpAddress = "192.168.1.1",
            UserAgent = "Chrome Browser",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        var mobileSession = new IdentityUserSession
        {
            UserId = user.Id,
            SessionToken = "mobile-session-token",
            SessionType = SessionType.Mobile,
            IpAddress = "192.168.1.2",
            DeviceFingerprint = "iPhone 15"
        };

        _context.IdentityUserSessions.AddRange(webSession, mobileSession);
        await _context.SaveChangesAsync();

        // Assert - Verify sessions are created and linked
        var userSessions = await _context.IdentityUserSessions
            .Where(s => s.UserId == user.Id)
            .ToListAsync();

        Assert.AreEqual(2, userSessions.Count, "Both sessions should be created for user");
        Assert.IsTrue(userSessions.Any(s => s.SessionType == SessionType.Web), "Web session should exist");
        Assert.IsTrue(userSessions.Any(s => s.SessionType == SessionType.Mobile), "Mobile session should exist");

        var webSess = userSessions.First(s => s.SessionType == SessionType.Web);
        Assert.AreEqual("192.168.1.1", webSess.IpAddress, "Web session IP should be tracked");
        Assert.IsNotNull(webSess.ExpiresAt, "Web session should have expiry");
    }

    /// <summary>
    /// Test Case: 5-Level Permission System Integration
    /// Description: Verifies that the 5-level permission system works with database operations
    /// Acceptance Criteria:
    /// - Should support all 5 levels of permissions
    /// - Should maintain proper relationships between entities
    /// - Should support complex permission queries
    /// </summary>
    [TestMethod]
    public async Task FiveLevelPermissionSystem_Integration_WorksCorrectly()
    {
        // Arrange - Create base entities
        var user = new IdentityUser { Email = "perm@example.com", UserName = "permuser" };
        var role = new IdentityRole { Name = "TestRole" };
        var group = new IdentityGroup { Name = "TestGroup", Description = "Test group for permissions" };
        var permission = new IdentityPermission { Resource = "TestResource", Action = "TestAction" };

        _context.Users.Add(user);
        _context.Roles.Add(role);
        _context.IdentityGroups.Add(group);
        _context.IdentityPermissions.Add(permission);
        await _context.SaveChangesAsync();

        // Act - Create permission relationships (Level 1: Role, Level 2: Group, Level 3: User)
        var rolePermission = new IdentityRolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id,
            IsGranted = true,
            Priority = 0
        };

        var groupPermission = new IdentityGroupPermission
        {
            GroupId = group.Id,
            PermissionId = permission.Id,
            IsGranted = true,
            Priority = 50
        };

        var userPermission = new IdentityUserPermission
        {
            UserId = user.Id,
            PermissionId = permission.Id,
            IsGranted = false, // Deny at user level
            Priority = 100
        };

        _context.IdentityRolePermissions.Add(rolePermission);
        _context.IdentityGroupPermissions.Add(groupPermission);
        _context.IdentityUserPermissions.Add(userPermission);
        await _context.SaveChangesAsync();

        // Assert - Verify all permission levels are created
        var rolePerms = await _context.IdentityRolePermissions.CountAsync();
        var groupPerms = await _context.IdentityGroupPermissions.CountAsync();
        var userPerms = await _context.IdentityUserPermissions.CountAsync();

        Assert.AreEqual(1, rolePerms, "Role permission should be created");
        Assert.AreEqual(1, groupPerms, "Group permission should be created");
        Assert.AreEqual(1, userPerms, "User permission should be created");

        // Assert - Verify priority system is maintained
        var allPermissions = await _context.IdentityUserPermissions
            .Include(up => up.Permission)
            .Where(up => up.Permission.Resource == "TestResource")
            .ToListAsync();

        Assert.IsTrue(allPermissions.Any(), "User permissions should be queryable with includes");
    }


    /// <summary>
    /// Test Case: Enterprise Audit Trail
    /// Description: Verifies that audit fields are properly maintained across entities
    /// Acceptance Criteria:
    /// - Should track creation and modification timestamps
    /// - Should support user tracking for all entities
    /// - Should maintain audit consistency across operations
    /// </summary>
    [TestMethod]
    public async Task EnterpriseAuditTrail_Tracking_WorksCorrectly()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var currentTime = DateTime.UtcNow;

        var user = new IdentityUser
        {
            Email = "audit@example.com",
            UserName = "audituser",
            CreatedBy = adminUserId,
            UpdatedBy = adminUserId
        };

        var permission = new IdentityPermission
        {
            Resource = "AuditResource",
            Action = "AuditAction",
            CreatedBy = adminUserId,
            UpdatedBy = adminUserId
        };

        // Act
        _context.Users.Add(user);
        _context.IdentityPermissions.Add(permission);
        await _context.SaveChangesAsync();

        // Assert - Verify audit fields are maintained
        var auditedUser = await _context.Users.FirstAsync(u => u.Email == "audit@example.com");
        var auditedPermission = await _context.IdentityPermissions.FirstAsync(p => p.Resource == "AuditResource");

        Assert.AreEqual(adminUserId, auditedUser.CreatedBy, "User CreatedBy should be tracked");
        Assert.AreEqual(adminUserId, auditedUser.UpdatedBy, "User UpdatedBy should be tracked");
        Assert.IsTrue(auditedUser.CreatedAt >= currentTime.AddMinutes(-1), "User CreatedAt should be recent");
        Assert.IsTrue(auditedUser.UpdatedAt >= currentTime.AddMinutes(-1), "User UpdatedAt should be recent");

        Assert.AreEqual(adminUserId, auditedPermission.CreatedBy, "Permission CreatedBy should be tracked");
        Assert.AreEqual(adminUserId, auditedPermission.UpdatedBy, "Permission UpdatedBy should be tracked");
        Assert.IsTrue(auditedPermission.IsActive, "New permissions should be active by default");
    }
}