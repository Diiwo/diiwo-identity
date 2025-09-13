using Diiwo.Identity.App.Entities;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.App.Tests.Entities;

/// <summary>
/// Test suite for AppUserSession entity
/// Validates session management, expiration logic, and validation rules
/// </summary>
[TestClass]
public class AppUserSessionTests
{
    /// <summary>
    /// Test Case: AppUserSession Constructor Initialization
    /// Description: Verifies that AppUserSession constructor sets all default values correctly
    /// Acceptance Criteria:
    /// - Session ID should be automatically generated and not empty
    /// - SessionToken should be generated and not empty when provided
    /// - SessionType should default to Web
    /// - IsActive should default to true
    /// - ExpiresAt should be set to future date (default 24 hours)
    /// </summary>
    [TestMethod]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var session = new AppUserSession
        {
            SessionToken = "test-token"
        };

        // Assert
        Assert.AreNotEqual(Guid.Empty, session.Id, "Session ID should be automatically generated");
        Assert.IsNotNull(session.SessionToken, "SessionToken should not be null");
        Assert.AreNotEqual(string.Empty, session.SessionToken, "SessionToken should not be empty");
        Assert.AreEqual(SessionType.Web, session.SessionType, "SessionType should default to Web");
        Assert.IsTrue(session.IsActive, "IsActive should default to true");
        Assert.IsTrue(session.ExpiresAt > DateTime.UtcNow, "ExpiresAt should be in the future");
    }

    /// <summary>
    /// Test Case: IsExpired Property with Future Expiration
    /// Description: Verifies IsExpired property returns false when expiration is in the future
    /// Acceptance Criteria:
    /// - Should return false when ExpiresAt is greater than current UTC time
    /// - Should allow continued session usage
    /// - Should use accurate time comparison
    /// </summary>
    [TestMethod]
    public void IsExpired_WithFutureExpiration_ReturnsFalse()
    {
        // Arrange
        var session = new AppUserSession
        {
            SessionToken = "test-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var isExpired = session.IsExpired;

        // Assert
        Assert.IsFalse(isExpired, "IsExpired should return false when expiration is in the future");
    }

    /// <summary>
    /// Test Case: IsExpired Property with Past Expiration
    /// Description: Verifies IsExpired property returns true when expiration is in the past
    /// Acceptance Criteria:
    /// - Should return true when ExpiresAt is less than current UTC time
    /// - Should prevent continued session usage
    /// - Should handle expired sessions correctly
    /// </summary>
    [TestMethod]
    public void IsExpired_WithPastExpiration_ReturnsTrue()
    {
        // Arrange
        var session = new AppUserSession
        {
            SessionToken = "test-token",
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var isExpired = session.IsExpired;

        // Assert
        Assert.IsTrue(isExpired, "IsExpired should return true when expiration is in the past");
    }

    /// <summary>
    /// Test Case: IsValid Property when Active and Not Expired
    /// Description: Verifies IsValid property returns true when session is both active and not expired
    /// Acceptance Criteria:
    /// - Should return true when IsActive is true AND IsExpired is false
    /// - Should allow session usage for valid sessions
    /// - Should combine both validation conditions
    /// </summary>
    [TestMethod]
    public void IsValid_WhenActiveAndNotExpired_ReturnsTrue()
    {
        // Arrange
        var session = new AppUserSession
        {
            SessionToken = "test-token",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var isValid = session.IsValid;

        // Assert
        Assert.IsTrue(isValid, "IsValid should return true when session is active and not expired");
    }

    /// <summary>
    /// Test Case: IsValid Property when Inactive
    /// Description: Verifies IsValid property returns false when session is inactive
    /// Acceptance Criteria:
    /// - Should return false when IsActive is false (regardless of expiration)
    /// - Should prevent session usage for inactive sessions
    /// - Should handle manually deactivated sessions
    /// </summary>
    [TestMethod]
    public void IsValid_WhenInactive_ReturnsFalse()
    {
        // Arrange
        var session = new AppUserSession
        {
            SessionToken = "test-token",
            IsActive = false,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var isValid = session.IsValid;

        // Assert
        Assert.IsFalse(isValid, "IsValid should return false when session is inactive");
    }

    /// <summary>
    /// Test Case: IsValid Property when Expired
    /// Description: Verifies IsValid property returns false when session is expired
    /// Acceptance Criteria:
    /// - Should return false when IsExpired is true (regardless of active status)
    /// - Should prevent session usage for expired sessions
    /// - Should handle automatic expiration correctly
    /// </summary>
    [TestMethod]
    public void IsValid_WhenExpired_ReturnsFalse()
    {
        // Arrange
        var session = new AppUserSession
        {
            SessionToken = "test-token",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var isValid = session.IsValid;

        // Assert
        Assert.IsFalse(isValid, "IsValid should return false when session is expired");
    }

    /// <summary>
    /// Test Case: Session with Different Types
    /// Description: Verifies session can be created with different session types
    /// Acceptance Criteria:
    /// - Should accept Web, Mobile, API, and Desktop session types
    /// - Should maintain session type setting correctly
    /// - Should support different client types
    /// </summary>
    [TestMethod]
    public void Session_WithDifferentTypes_SetsCorrectly()
    {
        // Arrange & Act
        var webSession = new AppUserSession
        {
            SessionToken = "web-token",
            SessionType = SessionType.Web
        };

        var mobileSession = new AppUserSession
        {
            SessionToken = "mobile-token",
            SessionType = SessionType.Mobile
        };

        var apiSession = new AppUserSession
        {
            SessionToken = "api-token",
            SessionType = SessionType.API
        };

        var desktopSession = new AppUserSession
        {
            SessionToken = "desktop-token",
            SessionType = SessionType.Desktop
        };

        // Assert
        Assert.AreEqual(SessionType.Web, webSession.SessionType, "Web session type should be set correctly");
        Assert.AreEqual(SessionType.Mobile, mobileSession.SessionType, "Mobile session type should be set correctly");
        Assert.AreEqual(SessionType.API, apiSession.SessionType, "API session type should be set correctly");
        Assert.AreEqual(SessionType.Desktop, desktopSession.SessionType, "Desktop session type should be set correctly");
    }

    /// <summary>
    /// Test Case: Session with Tracking Information
    /// Description: Verifies session properly stores client tracking information
    /// Acceptance Criteria:
    /// - Should store IP address for security tracking
    /// - Should store User Agent for client identification
    /// - Should handle null values gracefully
    /// - Should support audit trail requirements
    /// </summary>
    [TestMethod]
    public void Session_WithTrackingInfo_StoresCorrectly()
    {
        // Arrange & Act
        var session = new AppUserSession
        {
            SessionToken = "tracked-token",
            IpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            SessionType = SessionType.Web
        };

        // Assert
        Assert.AreEqual("192.168.1.100", session.IpAddress, "IP address should be stored correctly");
        Assert.AreEqual("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36", session.UserAgent, "User agent should be stored correctly");
        Assert.AreEqual("tracked-token", session.SessionToken, "Session token should be stored correctly");
    }

    /// <summary>
    /// Test Case: Session Activity Tracking
    /// Description: Verifies session can track last activity time
    /// Acceptance Criteria:
    /// - Should allow setting LastActivityAt timestamp
    /// - Should support session activity monitoring
    /// - Should handle null activity time for new sessions
    /// </summary>
    [TestMethod]
    public void Session_ActivityTracking_WorksCorrectly()
    {
        // Arrange
        var activityTime = DateTime.UtcNow;
        var session = new AppUserSession
        {
            SessionToken = "activity-token",
            LastActivityAt = activityTime
        };

        // Act & Assert
        Assert.AreEqual(activityTime, session.LastActivityAt, "LastActivityAt should be stored correctly");
        
        // Test null activity (new session)
        var newSession = new AppUserSession
        {
            SessionToken = "new-token"
        };
        Assert.IsNull(newSession.LastActivityAt, "New session should have null LastActivityAt");
    }
}