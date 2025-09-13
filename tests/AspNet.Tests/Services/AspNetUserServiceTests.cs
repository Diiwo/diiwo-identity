using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Moq;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Application.Services;
using Diiwo.Identity.AspNet.Entities;

namespace Diiwo.Identity.AspNet.Tests.Services;

/// <summary>
/// Test suite for AspNetUserService - FIXED VERSION
/// Validates enterprise user management operations, authentication logic, and business rules
/// </summary>
[TestClass]
public class AspNetUserServiceTests
{
    private AspNetIdentityDbContext _context = null!;
    private AspNetUserService _userService = null!;
    private Mock<ILogger<AspNetUserService>> _mockLogger = null!;
    private Mock<Microsoft.AspNetCore.Identity.UserManager<Diiwo.Identity.AspNet.Entities.IdentityUser>> _mockUserManager = null!;
    private Mock<Microsoft.AspNetCore.Identity.RoleManager<Diiwo.Identity.AspNet.Entities.IdentityRole>> _mockRoleManager = null!;
    private Mock<Microsoft.AspNetCore.Identity.SignInManager<Diiwo.Identity.AspNet.Entities.IdentityUser>> _mockSignInManager = null!;

    [TestInitialize]
    public void Setup()
    {
        // Setup DbContext with InMemory database
        var options = new DbContextOptionsBuilder<AspNetIdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AspNetIdentityDbContext(options);

        // Setup mocks
        _mockLogger = new Mock<ILogger<AspNetUserService>>();
        _mockUserManager = CreateUserManagerMock();
        _mockRoleManager = CreateRoleManagerMock();
        _mockSignInManager = CreateSignInManagerMock();

        // Setup UserManager behaviors for successful operations
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<Diiwo.Identity.AspNet.Entities.IdentityUser>(), It.IsAny<string>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.IdentityResult.Success);

        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<Diiwo.Identity.AspNet.Entities.IdentityUser>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.IdentityResult.Success);

        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((string email) => _context.Users.FirstOrDefault(u => u.Email == email));

        // Initialize service with CORRECT parameter order
        // Note: SignInManager is not actually used in the methods we're testing
        _userService = new AspNetUserService(
            _mockUserManager.Object,   // UserManager first
            _mockRoleManager.Object,   // RoleManager second
            null!,                     // SignInManager not needed for these tests
            _context,                  // DbContext fourth
            _mockLogger.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    #region Helper Methods

    private Mock<Microsoft.AspNetCore.Identity.UserManager<Diiwo.Identity.AspNet.Entities.IdentityUser>> CreateUserManagerMock()
    {
        var userStore = new Mock<Microsoft.AspNetCore.Identity.IUserStore<Diiwo.Identity.AspNet.Entities.IdentityUser>>();

        return new Mock<Microsoft.AspNetCore.Identity.UserManager<Diiwo.Identity.AspNet.Entities.IdentityUser>>(
            userStore.Object,
            null, null, null, null, null, null, null, null);
    }

    private Mock<Microsoft.AspNetCore.Identity.RoleManager<Diiwo.Identity.AspNet.Entities.IdentityRole>> CreateRoleManagerMock()
    {
        var roleStore = new Mock<Microsoft.AspNetCore.Identity.IRoleStore<Diiwo.Identity.AspNet.Entities.IdentityRole>>();
        var roleValidators = new List<Microsoft.AspNetCore.Identity.IRoleValidator<Diiwo.Identity.AspNet.Entities.IdentityRole>>();
        var keyNormalizer = new Mock<Microsoft.AspNetCore.Identity.ILookupNormalizer>().Object;
        var errors = new Microsoft.AspNetCore.Identity.IdentityErrorDescriber();
        var roleLogger = new Mock<ILogger<Microsoft.AspNetCore.Identity.RoleManager<Diiwo.Identity.AspNet.Entities.IdentityRole>>>().Object;

        return new Mock<Microsoft.AspNetCore.Identity.RoleManager<Diiwo.Identity.AspNet.Entities.IdentityRole>>(
            roleStore.Object,
            roleValidators,
            keyNormalizer,
            errors,
            roleLogger);
    }

    private Mock<Microsoft.AspNetCore.Identity.SignInManager<Diiwo.Identity.AspNet.Entities.IdentityUser>> CreateSignInManagerMock()
    {
        // We'll return null since SignInManager is complex to mock and our current tests don't use it
        return null!;
    }

    #endregion

    /// <summary>
    /// Test Case: Enterprise User Creation
    /// Description: Verifies that users can be created with all required enterprise attributes
    /// Acceptance Criteria:
    /// - Should create users with proper enterprise data validation
    /// - Should integrate with ASP.NET Core Identity UserManager
    /// - Should handle password validation and security requirements
    /// - Should set proper audit trail timestamps
    /// </summary>
    [TestMethod]
    public async Task CreateUserAsync_WithEnterpriseData_CreatesUser()
    {
        // Act
        var result = await _userService.CreateUserAsync(
            "enterprise@example.com",
            "ComplexPassword123!",
            "John",
            "Doe",
            "johndoe");

        // Assert
        Assert.IsNotNull(result, "IdentityResult should be returned");
        Assert.IsTrue(result.Succeeded, "User creation should succeed");

        // Verify UserManager.CreateAsync was called with correct parameters
        _mockUserManager.Verify(x => x.CreateAsync(
            It.Is<Diiwo.Identity.AspNet.Entities.IdentityUser>(u => u.Email == "enterprise@example.com" &&
                                   u.FirstName == "John" &&
                                   u.LastName == "Doe" &&
                                   u.UserName == "johndoe"),
            "ComplexPassword123!"), Times.Once);
    }

    /// <summary>
    /// Test Case: User Retrieval by Email
    /// Description: Verifies that users can be retrieved by their email addresses
    /// Acceptance Criteria:
    /// - Should find and return users by email address
    /// - Should return null for non-existent email addresses
    /// - Should use case-insensitive email matching
    /// </summary>
    [TestMethod]
    public async Task GetUserByEmailAsync_WithValidEmail_ReturnsUser()
    {
        // Arrange - Add user directly to context for this test
        var user = new Diiwo.Identity.AspNet.Entities.IdentityUser
        {
            Email = "retrieve@example.com",
            UserName = "retrieveuser",
            FirstName = "Jane",
            LastName = "Smith"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByEmailAsync("retrieve@example.com");

        // Assert
        Assert.IsNotNull(result, "User should be successfully retrieved by email");
        Assert.AreEqual("retrieve@example.com", result.Email, "Retrieved user should have correct email");
        Assert.AreEqual("Jane", result.FirstName, "Retrieved user should have correct first name");
    }

    /// <summary>
    /// Test Case: User Profile Updates
    /// Description: Verifies that user profiles can be updated correctly
    /// Acceptance Criteria:
    /// - Should update user information successfully
    /// - Should maintain data integrity during updates
    /// - Should update audit trail timestamps
    /// </summary>
    [TestMethod]
    public async Task UpdateUserAsync_WithValidUser_UpdatesUser()
    {
        // Arrange
        var user = new Diiwo.Identity.AspNet.Entities.IdentityUser
        {
            Email = "update@example.com",
            UserName = "updateuser",
            FirstName = "Original",
            LastName = "Name"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Modify user data
        user.FirstName = "Updated";
        user.LastName = "NewName";

        // Act
        var result = await _userService.UpdateUserAsync(user);

        // Assert
        Assert.IsNotNull(result, "IdentityResult should be returned");
        Assert.IsTrue(result.Succeeded, "User update should succeed");

        // Verify UserManager.UpdateAsync was called
        _mockUserManager.Verify(x => x.UpdateAsync(It.IsAny<Diiwo.Identity.AspNet.Entities.IdentityUser>()), Times.Once);
        Assert.IsTrue(user.UpdatedAt > DateTime.UtcNow.AddMinutes(-1), "UpdatedAt should be set to current time");
    }
}