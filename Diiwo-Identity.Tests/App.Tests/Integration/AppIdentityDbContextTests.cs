using Microsoft.EntityFrameworkCore;
using Diiwo.Identity.App.DbContext;
using Diiwo.Identity.App.Entities;
using Diiwo.Identity.Shared.Enums;

namespace App.Tests.Integration;

/// <summary>
/// Test suite for AppIdentityDbContext integration
/// Validates database operations, entity relationships, and data seeding
/// </summary>
[TestClass]
public class AppIdentityDbContextTests
{
    private AppIdentityDbContext _context = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppIdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppIdentityDbContext(options);
        _context.Database.EnsureCreated();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    /// <summary>
    /// Test Case: Database User CRUD Operations
    /// Description: Verifies that users can be created, saved, and retrieved from the database
    /// Acceptance Criteria:
    /// - User should be successfully persisted to database
    /// - Retrieved user should match saved user properties
    /// - All user fields should be correctly stored and retrieved
    /// </summary>
    [TestMethod]
    public async Task CanCreateAndRetrieveUser()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "integration@example.com",
            PasswordHash = "hashed-password",
            FirstName = "Integration",
            LastName = "Test"
        };

        // Act
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var retrievedUser = await _context.Users.FindAsync(user.Id);

        // Assert
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual("integration@example.com", retrievedUser.Email);
        Assert.AreEqual("Integration", retrievedUser.FirstName);
        Assert.AreEqual("Test", retrievedUser.LastName);
    }

    /// <summary>
    /// Test Case: Permission System Integration
    /// Description: Verifies that complex permission relationships can be established and maintained
    /// Acceptance Criteria:
    /// - Permissions should be correctly associated with roles
    /// - Users should be correctly associated with roles
    /// - Navigation properties should work correctly
    /// - All relationships should be properly persisted
    /// </summary>
    [TestMethod]
    public async Task CanCreatePermissionHierarchy()
    {
        // Arrange
        var permission = new AppPermission
        {
            Resource = "TestResource",
            Action = "TestAction",
            Scope = PermissionScope.Global
        };

        var role = new AppRole
        {
            Name = "TestRole",
            Description = "Test role"
        };

        var user = new AppUser
        {
            Email = "test@example.com",
            PasswordHash = "hashed-password"
        };

        var group = new AppGroup
        {
            Name = "TestGroup",
            Description = "Test group"
        };

        // Act
        _context.Permissions.Add(permission);
        _context.Roles.Add(role);
        _context.Users.Add(user);
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        var rolePermission = new AppRolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id,
            IsGranted = true,
            Priority = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var groupPermission = new AppGroupPermission
        {
            GroupId = group.Id,
            PermissionId = permission.Id,
            IsGranted = true,
            Priority = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var userPermission = new AppUserPermission
        {
            UserId = user.Id,
            PermissionId = permission.Id,
            IsGranted = true,
            Priority = 100,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.RolePermissions.Add(rolePermission);
        _context.GroupPermissions.Add(groupPermission);
        _context.UserPermissions.Add(userPermission);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedRolePermission = await _context.RolePermissions
            .Include(rp => rp.Role)
            .Include(rp => rp.Permission)
            .FirstOrDefaultAsync(rp => rp.RoleId == role.Id);

        var retrievedGroupPermission = await _context.GroupPermissions
            .Include(gp => gp.Group)
            .Include(gp => gp.Permission)
            .FirstOrDefaultAsync(gp => gp.GroupId == group.Id);

        var retrievedUserPermission = await _context.UserPermissions
            .Include(up => up.User)
            .Include(up => up.Permission)
            .FirstOrDefaultAsync(up => up.UserId == user.Id);

        Assert.IsNotNull(retrievedRolePermission);
        Assert.AreEqual("TestRole", retrievedRolePermission.Role.Name);
        Assert.AreEqual("TestResource", retrievedRolePermission.Permission.Resource);

        Assert.IsNotNull(retrievedGroupPermission);
        Assert.AreEqual("TestGroup", retrievedGroupPermission.Group.Name);
        Assert.AreEqual("TestAction", retrievedGroupPermission.Permission.Action);

        Assert.IsNotNull(retrievedUserPermission);
        Assert.AreEqual("test@example.com", retrievedUserPermission.User.Email);
        Assert.IsTrue(retrievedUserPermission.IsGranted);
    }

    /// <summary>
    /// Test Case: User Session Management Integration
    /// Description: Verifies that user sessions can be created and properly associated with users
    /// Acceptance Criteria:
    /// - Sessions should be correctly linked to users
    /// - Session data should be persisted correctly
    /// - Navigation properties should work for user-session relationships
    /// </summary>
    [TestMethod]
    public async Task CanCreateUserSession()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "session@example.com",
            PasswordHash = "hashed-password"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var session = new AppUserSession
        {
            UserId = user.Id,
            SessionToken = "test-session-token",
            SessionType = SessionType.Mobile,
            IpAddress = "192.168.1.100",
            UserAgent = "Test Mobile App",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act
        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        var retrievedSession = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionToken == "test-session-token");

        // Assert
        Assert.IsNotNull(retrievedSession);
        Assert.AreEqual(user.Id, retrievedSession.UserId);
        Assert.AreEqual("session@example.com", retrievedSession.User.Email);
        Assert.AreEqual(SessionType.Mobile, retrievedSession.SessionType);
        Assert.AreEqual("192.168.1.100", retrievedSession.IpAddress);
        Assert.IsTrue(retrievedSession.IsValid);
    }

    /// <summary>
    /// Test Case: Login History Tracking Integration
    /// Description: Verifies that login history records can be created and associated with users
    /// Acceptance Criteria:
    /// - Login history should be correctly linked to users
    /// - Authentication method and attempt details should be stored
    /// - Successful and failed login attempts should be tracked properly
    /// </summary>
    [TestMethod]
    public async Task CanCreateLoginHistory()
    {
        // Arrange
        var user = new AppUser
        {
            Email = "history@example.com",
            PasswordHash = "hashed-password"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginHistory = new AppUserLoginHistory
        {
            UserId = user.Id,
            IsSuccessful = true,
            AuthMethod = AuthMethod.EmailPassword,
            IpAddress = "10.0.0.1",
            UserAgent = "Test Browser",
            LoginAttemptAt = DateTime.UtcNow
        };

        // Act
        _context.LoginHistory.Add(loginHistory);
        await _context.SaveChangesAsync();

        var retrievedHistory = await _context.LoginHistory
            .Include(h => h.User)
            .FirstOrDefaultAsync(h => h.UserId == user.Id);

        // Assert
        Assert.IsNotNull(retrievedHistory);
        Assert.AreEqual(user.Id, retrievedHistory.UserId);
        Assert.AreEqual("history@example.com", retrievedHistory.User.Email);
        Assert.IsTrue(retrievedHistory.IsSuccessful);
        Assert.AreEqual(AuthMethod.EmailPassword, retrievedHistory.AuthMethod);
        Assert.AreEqual("10.0.0.1", retrievedHistory.IpAddress);
    }

    [TestMethod]
    public async Task SeedData_CreatesDefaultRolesAndPermissions()
    {
        // Act - seed data should be created when context is initialized

        // Assert
        var roles = await _context.Roles.ToListAsync();
        var permissions = await _context.Permissions.ToListAsync();
        var rolePermissions = await _context.RolePermissions.ToListAsync();

        Assert.IsTrue(roles.Count >= 3, "Should have at least 3 default roles");
        Assert.IsTrue(permissions.Count >= 6, "Should have at least 6 default permissions");
        Assert.IsTrue(rolePermissions.Count > 0, "Should have default role permissions");

        var superAdminRole = roles.FirstOrDefault(r => r.Name == "SuperAdmin");
        Assert.IsNotNull(superAdminRole, "Should have SuperAdmin role");

        var userRole = roles.FirstOrDefault(r => r.Name == "User");
        Assert.IsNotNull(userRole, "Should have User role");

        var readPermission = permissions.FirstOrDefault(p => p.Resource == "User" && p.Action == "Read");
        Assert.IsNotNull(readPermission, "Should have User.Read permission");
    }
}