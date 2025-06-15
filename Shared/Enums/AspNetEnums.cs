using System.ComponentModel;

namespace Diiwo.Identity.Shared.Enums;

/// <summary>
/// AspNet-specific enums for enterprise features
/// </summary>
public static class AspNetEnums
{
    /// <summary>
    /// Session states for AspNet architecture  
    /// </summary>
    public enum SessionState
    {
        [Description("Active session")]
        Active = 0,

        [Description("Expired session")]
        Expired = 1,

        [Description("Terminated session")]
        Terminated = 2,

        [Description("Suspended session")]
        Suspended = 3
    }

    /// <summary>
    /// Login history entry states
    /// </summary>
    public enum LoginHistoryState
    {
        [Description("Successful login")]
        Successful = 0,

        [Description("Failed login")]
        Failed = 1,

        [Description("Blocked login")]
        Blocked = 2
    }

    /// <summary>
    /// User lock states for AspNet Identity integration
    /// </summary>
    public enum UserLockState
    {
        [Description("User is unlocked")]
        Unlocked = 0,

        [Description("User is temporarily locked")]
        TemporaryLock = 1,

        [Description("User is permanently locked")]
        PermanentLock = 2,

        [Description("User is suspended")]
        Suspended = 3
    }

    /// <summary>
    /// Permission grant states for AspNet architecture
    /// </summary>
    public enum PermissionGrantState
    {
        [Description("Permission is granted")]
        Granted = 0,

        [Description("Permission is denied")]
        Denied = 1,

        [Description("Permission is inherited")]
        Inherited = 2,

        [Description("Permission is expired")]
        Expired = 3
    }
}