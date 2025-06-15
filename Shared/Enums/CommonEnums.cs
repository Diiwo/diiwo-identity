using System.ComponentModel;

namespace Diiwo.Identity.Shared.Enums;

/// <summary>
/// Permission scopes for granular access control (shared between both architectures)
/// </summary>
public enum PermissionScope
{
    [Description("Global system-wide permission")]
    Global = 0,

    [Description("Model/Resource-level permission")]
    Model = 1,

    [Description("Object/Instance-level permission")]
    Object = 2
}

/// <summary>
/// Authentication methods (shared between both architectures)
/// </summary>
public enum AuthMethod
{
    [Description("Standard email/password authentication")]
    EmailPassword = 0,
    
    [Description("Standard password authentication (alias for EmailPassword)")]
    Password = 0,

    [Description("OAuth authentication")]
    OAuth = 1,

    [Description("Single Sign-On (SSO)")]
    SSO = 2,

    [Description("Multi-factor authentication")]
    MFA = 3,

    [Description("Biometric authentication")]
    Biometric = 4
}

/// <summary>
/// Session types (shared between both architectures)
/// </summary>
public enum SessionType
{
    [Description("Web browser session")]
    Web = 0,

    [Description("Mobile application session")]
    Mobile = 1,

    [Description("API token session")]
    API = 2,

    [Description("Desktop application session")]
    Desktop = 3
}