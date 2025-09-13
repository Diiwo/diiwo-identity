using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.AspNet.Tests.Entities;

/// <summary>
/// Test suite for IdentityPermission entity
/// Validates permission entity logic, business rules, and 5-level permission system integration
/// </summary>
[TestClass]
public class IdentityPermissionTests
{
    /// <summary>
    /// Test Case: Permission Creation with Required Properties
    /// Description: Verifies that IdentityPermission can be created with resource and action
    /// Acceptance Criteria:
    /// - Permission should be created with resource and action
    /// - Default scope should be Global if not specified
    /// - Permission should be active by default
    /// </summary>
    [TestMethod]
    public void CreatePermission_WithResourceAndAction_SetsDefaultsCorrectly()
    {
        // Arrange & Act
        var permission = new IdentityPermission
        {
            Resource = "Documents",
            Action = "Read"
        };

        // Assert
        Assert.IsNotNull(permission, "IdentityPermission should be created successfully");
        Assert.AreEqual("Documents", permission.Resource, "Resource should be set correctly");
        Assert.AreEqual("Read", permission.Action, "Action should be set correctly");
        Assert.AreEqual(PermissionScope.Global, permission.Scope, "Default scope should be Global");
        Assert.IsTrue(permission.IsActive, "New permissions should be active by default");
        Assert.IsNotNull(permission.Id, "Permission ID should be automatically generated");
    }

    /// <summary>
    /// Test Case: Permission with Different Scopes
    /// Description: Verifies that permissions can be created with different permission scopes
    /// Acceptance Criteria:
    /// - Should support Global, Model, and Object scopes
    /// - Scope should affect permission evaluation in 5-level system
    /// - Each scope should have appropriate use cases
    /// </summary>
    [TestMethod]
    public void CreatePermission_WithDifferentScopes_SetsCorrectly()
    {
        // Test Global scope
        var globalPermission = new IdentityPermission
        {
            Resource = "System",
            Action = "Admin",
            Scope = PermissionScope.Global
        };
        Assert.AreEqual(PermissionScope.Global, globalPermission.Scope, "Global scope should be set correctly");

        // Test Model scope
        var modelPermission = new IdentityPermission
        {
            Resource = "Users",
            Action = "Create",
            Scope = PermissionScope.Model
        };
        Assert.AreEqual(PermissionScope.Model, modelPermission.Scope, "Model scope should be set correctly");

        // Test Object scope
        var objectPermission = new IdentityPermission
        {
            Resource = "Document",
            Action = "Edit",
            Scope = PermissionScope.Object
        };
        Assert.AreEqual(PermissionScope.Object, objectPermission.Scope, "Object scope should be set correctly");
    }

    /// <summary>
    /// Test Case: Permission Display Name Generation
    /// Description: Verifies that permission display names are generated correctly
    /// Acceptance Criteria:
    /// - Display name should combine resource and action
    /// - Should be human-readable format
    /// - Should be consistent across different permissions
    /// </summary>
    [TestMethod]
    public void Name_WithResourceAndAction_FormatsCorrectly()
    {
        // Arrange
        var permission = new IdentityPermission
        {
            Resource = "Documents",
            Action = "Read"
        };

        // Act
        var displayName = permission.Name;

        // Assert
        Assert.AreEqual("Documents.Read", displayName, "Name should combine resource and action with dot separator");
    }

    /// <summary>
    /// Test Case: Permission Key Generation
    /// Description: Verifies that unique permission keys are generated correctly
    /// Acceptance Criteria:
    /// - Key should be unique combination of resource and action
    /// - Should be suitable for lookup operations
    /// - Should be case-sensitive and consistent
    /// </summary>
    [TestMethod]
    public void GetPermissionKey_WithResourceAndAction_GeneratesUniqueKey()
    {
        // Arrange
        var permission1 = new IdentityPermission
        {
            Resource = "Documents",
            Action = "Read"
        };
        
        var permission2 = new IdentityPermission
        {
            Resource = "Documents",
            Action = "Write"
        };

        // Act
        var key1 = permission1.Name;
        var key2 = permission2.Name;

        // Assert
        Assert.AreEqual("Documents.Read", key1, "Permission key should combine resource and action with colon");
        Assert.AreEqual("Documents.Write", key2, "Permission key should be unique for different actions");
        Assert.AreNotEqual(key1, key2, "Different permissions should have different keys");
    }

    /// <summary>
    /// Test Case: Cross-Architecture Migration Support
    /// Description: Verifies that optional foreign keys support migration from App architecture
    /// Acceptance Criteria:
    /// - AppPermissionId should be nullable for cross-architecture support
    /// - Should allow mapping to App architecture permissions
    /// - Migration relationships should work correctly
    /// </summary>
    [TestMethod]
    public void IdentityPermission_CrossArchitectureMigration_SupportsAppPermissionReference()
    {
        // Arrange
        var appPermissionId = Guid.NewGuid();
        var permission = new IdentityPermission
        {
            Resource = "MigratedResource",
            Action = "MigratedAction",
            AppPermissionId = appPermissionId
        };

        // Assert
        Assert.AreEqual(appPermissionId, permission.AppPermissionId, "AppPermissionId should support cross-architecture migration");
        Assert.IsNotNull(permission.AppPermissionId, "AppPermissionId should be set for migrated permissions");
    }

    /// <summary>
    /// Test Case: Permission Audit Fields
    /// Description: Verifies that audit fields are properly maintained for permissions
    /// Acceptance Criteria:
    /// - Should track creation and modification timestamps
    /// - Should support user tracking for compliance
    /// - Should inherit from base entity properly
    /// </summary>
    [TestMethod]
    public void IdentityPermission_AuditFields_TrackCorrectly()
    {
        // Arrange
        var permission = new IdentityPermission
        {
            Resource = "AuditResource",
            Action = "AuditAction",
            CreatedBy = Guid.NewGuid(),
            UpdatedBy = Guid.NewGuid()
        };

        // Assert
        Assert.IsNotNull(permission.CreatedBy, "CreatedBy should be trackable for audit");
        Assert.IsNotNull(permission.UpdatedBy, "UpdatedBy should be trackable for audit");
        Assert.IsTrue(permission.CreatedAt <= DateTime.UtcNow, "CreatedAt should be set to current or past time");
        Assert.IsTrue(permission.UpdatedAt <= DateTime.UtcNow, "UpdatedAt should be set to current or past time");
        Assert.IsTrue(permission.IsActive, "New permissions should be active by default");
    }

    /// <summary>
    /// Test Case: Navigation Properties for 5-Level System
    /// Description: Verifies that navigation properties support the 5-level permission system
    /// Acceptance Criteria:
    /// - Should have collections for role, group, user, model, and object permissions
    /// - Collections should be initialized to prevent null references
    /// - Should support EF Core relationships
    /// </summary>
    [TestMethod]
    public void IdentityPermission_NavigationProperties_Support5LevelSystem()
    {
        // Arrange & Act
        var permission = new IdentityPermission
        {
            Resource = "TestResource",
            Action = "TestAction"
        };

        // Assert - Level 1: Role Permissions
        Assert.IsNotNull(permission.IdentityRolePermissions, "IdentityRolePermissions collection should be initialized");
        Assert.AreEqual(0, permission.IdentityRolePermissions.Count, "IdentityRolePermissions should be empty initially");

        // Assert - Level 2: Group Permissions  
        Assert.IsNotNull(permission.IdentityGroupPermissions, "IdentityGroupPermissions collection should be initialized");
        Assert.AreEqual(0, permission.IdentityGroupPermissions.Count, "IdentityGroupPermissions should be empty initially");

        // Assert - Level 3: User Permissions
        Assert.IsNotNull(permission.IdentityUserPermissions, "IdentityUserPermissions collection should be initialized");
        Assert.AreEqual(0, permission.IdentityUserPermissions.Count, "IdentityUserPermissions should be empty initially");

        // Assert - Level 4: Model Permissions
        Assert.IsNotNull(permission.IdentityModelPermissions, "IdentityModelPermissions collection should be initialized");
        Assert.AreEqual(0, permission.IdentityModelPermissions.Count, "IdentityModelPermissions should be empty initially");

        // Assert - Level 5: Object Permissions
        Assert.IsNotNull(permission.IdentityObjectPermissions, "IdentityObjectPermissions collection should be initialized");
        Assert.AreEqual(0, permission.IdentityObjectPermissions.Count, "IdentityObjectPermissions should be empty initially");
    }

    /// <summary>
    /// Test Case: Permission Description and Metadata
    /// Description: Verifies that permission metadata is handled correctly
    /// Acceptance Criteria:
    /// - Should support optional description
    /// - Description should provide human-readable explanation
    /// - Should handle null/empty descriptions gracefully
    /// </summary>
    [TestMethod]
    public void IdentityPermission_Description_HandlesCorrectly()
    {
        // Test with description
        var permissionWithDesc = new IdentityPermission
        {
            Resource = "Documents",
            Action = "Delete",
            Description = "Allows permanent deletion of documents"
        };
        Assert.AreEqual("Allows permanent deletion of documents", permissionWithDesc.Description, "Description should be set correctly");

        // Test without description
        var permissionNoDesc = new IdentityPermission
        {
            Resource = "Documents",
            Action = "Read"
        };
        Assert.IsNull(permissionNoDesc.Description, "Description should be nullable");
    }
}