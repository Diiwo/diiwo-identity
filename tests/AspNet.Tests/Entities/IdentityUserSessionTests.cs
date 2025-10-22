using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.AspNet.Tests.Entities;

/// <summary>
/// Test suite for IdentityUserSession entity
/// Validates session management logic, enterprise session tracking, and security features
/// </summary>
[TestClass]
public class IdentityUserSessionTests
{
    /// <summary>
    /// Test Case: Session Creation with Required Properties
    /// Description: Verifies that IdentityUserSession can be created with essential session data
    /// Acceptance Criteria:
    /// - Session should be created with user ID and session token
    /// - Default session type should be Web
    /// - Session should be active by default
    /// - Creation timestamp should be set
    /// </summary>
    [TestMethod]
    public void CreateSession_WithRequiredProperties_SetsDefaultsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionToken = "test-session-token-123";

        // Act
        var session = new IdentityUserSession
        {
            UserId = userId,
            SessionToken = sessionToken
        };

        // Assert
        Assert.IsNotNull(session, "IdentityUserSession should be created successfully");
        Assert.AreEqual(userId, session.UserId, "UserId should be set correctly");
        Assert.AreEqual(sessionToken, session.SessionToken, "SessionToken should be set correctly");
        Assert.AreEqual(SessionType.Web, session.SessionType, "Default session type should be Web");
        Assert.IsTrue(session.IsActive, "New sessions should be active by default");
        Assert.IsNotNull(session.Id, "Session ID should be automatically generated");
        Assert.IsTrue(session.CreatedAt <= DateTime.UtcNow, "CreatedAt should be set to current time or earlier");
    }

    /// <summary>
    /// Test Case: Session with Different Types
    /// Description: Verifies that sessions can be created with different session types
    /// Acceptance Criteria:
    /// - Should support Web, Mobile, API, and Desktop session types
    /// - Session type should be correctly set and maintained
    /// - Each type should be suitable for different client scenarios
    /// </summary>
    [TestMethod]
    public void CreateSession_WithDifferentTypes_SetsCorrectly()
    {
        var userId = Guid.NewGuid();

        // Test Web session
        var webSession = new IdentityUserSession
        {
            UserId = userId,
            SessionToken = "web-session-token",
            SessionType = SessionType.Web
        };
        Assert.AreEqual(SessionType.Web, webSession.SessionType, "Web session type should be set correctly");

        // Test Mobile session
        var mobileSession = new IdentityUserSession
        {
            UserId = userId,
            SessionToken = "mobile-session-token",
            SessionType = SessionType.Mobile
        };
        Assert.AreEqual(SessionType.Mobile, mobileSession.SessionType, "Mobile session type should be set correctly");

        // Test API session
        var apiSession = new IdentityUserSession
        {
            UserId = userId,
            SessionToken = "api-session-token",
            SessionType = SessionType.API
        };
        Assert.AreEqual(SessionType.API, apiSession.SessionType, "API session type should be set correctly");

        // Test Desktop session
        var desktopSession = new IdentityUserSession
        {
            UserId = userId,
            SessionToken = "desktop-session-token",
            SessionType = SessionType.Desktop
        };
        Assert.AreEqual(SessionType.Desktop, desktopSession.SessionType, "Desktop session type should be set correctly");
    }

    /// <summary>
    /// Test Case: Session Activity Validation
    /// Description: Verifies that session activity status can be determined correctly
    /// Acceptance Criteria:
    /// - Active sessions should be identified correctly
    /// - Expired sessions should be marked as inactive
    /// - Should handle sessions without expiry (persistent sessions)
    /// </summary>
    [TestMethod]
    public void IsSessionActive_WithDifferentStates_ValidatesCorrectly()
    {
        var userId = Guid.NewGuid();

        // Test active session (no expiry)
        var activeSession = new IdentityUserSession
        {
            UserId = userId,
            SessionToken = "active-session",
            State = Diiwo.Core.Domain.Enums.EntityState.Active
        };
        Assert.IsTrue(activeSession.IsActive, "Session without expiry should be active");

        // Test active session with future expiry
        var futureExpirySession = new IdentityUserSession
        {
            UserId = userId,
            SessionToken = "future-expiry-session",
            State = Diiwo.Core.Domain.Enums.EntityState.Active,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        Assert.IsTrue(futureExpirySession.IsActive, "Session with future expiry should be active");

        // Test inactive session
        var inactiveSession = new IdentityUserSession
        {
            UserId = userId,
            SessionToken = "inactive-session",
            State = Diiwo.Core.Domain.Enums.EntityState.Inactive
        };
        Assert.IsFalse(inactiveSession.IsActive, "Explicitly inactive session should be inactive");
    }

    /// <summary>
    /// Test Case: Session Security Information
    /// Description: Verifies that session security metadata is handled correctly
    /// Acceptance Criteria:
    /// - Should track IP address and user agent for security
    /// - Should handle device information for session management
    /// - Should support security audit trails
    /// </summary>
    [TestMethod]
    public void SessionSecurity_WithMetadata_TracksCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ipAddress = "192.168.1.100";
        var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
        var deviceInfo = "Windows Desktop Chrome";

        // Act
        var session = new IdentityUserSession
        {
            UserId = userId,
            SessionToken = "secure-session",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceFingerprint = deviceInfo
        };

        // Assert
        Assert.AreEqual(ipAddress, session.IpAddress, "IP address should be tracked for security");
        Assert.AreEqual(userAgent, session.UserAgent, "User agent should be tracked for security");
        Assert.AreEqual(deviceInfo, session.DeviceFingerprint, "Device info should be tracked for session management");
    }

    /// <summary>
    /// Test Case: Session Lifecycle Management
    /// Description: Verifies that session lifecycle timestamps are managed correctly
    /// Acceptance Criteria:
    /// - Should track last activity timestamp
    /// - Should support session refresh operations
    /// - Should handle session termination properly
    /// </summary>
    [TestMethod]
    public void SessionLifecycle_WithTimestamps_ManagesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var session = new IdentityUserSession
        {
            UserId = userId,
            SessionToken = "lifecycle-session"
        };

        // Act - Simulate activity update
        var lastActivity = DateTime.UtcNow.AddMinutes(-5);
        session.LastActivityAt = lastActivity;
        session.RefreshToken = "refresh-token-123";

        // Assert
        Assert.AreEqual(lastActivity, session.LastActivityAt, "Last activity should be tracked");
        Assert.AreEqual("refresh-token-123", session.RefreshToken, "Refresh token should be stored");
        Assert.IsTrue(session.CreatedAt <= DateTime.UtcNow, "Created timestamp should be valid");
    }


    /// <summary>
    /// Test Case: Session Expiration Management
    /// Description: Verifies that session expiration is handled correctly
    /// Acceptance Criteria:
    /// - Should support setting expiration dates
    /// - Should handle sessions with and without expiration
    /// - Should provide methods to check expiration status
    /// </summary>
    [TestMethod]
    public void SessionExpiration_WithDifferentExpiryDates_HandlesCorrectly()
    {
        var userId = Guid.NewGuid();

        // Test session with expiry
        var expiringSession = new IdentityUserSession
        {
            UserId = userId,
            SessionToken = "expiring-session",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        Assert.IsNotNull(expiringSession.ExpiresAt, "Session should support expiration dates");
        Assert.IsTrue(expiringSession.ExpiresAt > DateTime.UtcNow, "Expiry should be in the future");

        // Test persistent session (no expiry)
        var persistentSession = new IdentityUserSession
        {
            UserId = userId,
            SessionToken = "persistent-session"
        };
        Assert.IsNotNull(persistentSession.ExpiresAt, "Sessions should have default expiry for security");
        Assert.IsTrue(persistentSession.ExpiresAt > DateTime.UtcNow, "Default expiry should be in future");
    }

    /// <summary>
    /// Test Case: Navigation Properties
    /// Description: Verifies that navigation properties are properly initialized
    /// Acceptance Criteria:
    /// - Should maintain relationship to IdentityUser
    /// - Navigation properties should support EF Core relationships
    /// - Foreign key constraints should be properly maintained
    /// </summary>
    [TestMethod]
    public void IdentityUserSession_NavigationProperties_InitializedCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new IdentityUser
        {
            Id = userId,
            Email = "session-user@example.com",
            UserName = "sessionuser"
        };

        var session = new IdentityUserSession
        {
            UserId = userId,
            SessionToken = "nav-session",
            User = user
        };

        // Assert
        Assert.IsNotNull(session.User, "User navigation property should be available");
        Assert.AreEqual(user, session.User, "Navigation property should reference correct user");
        Assert.AreEqual(userId, session.UserId, "Foreign key should match user ID");
    }
}