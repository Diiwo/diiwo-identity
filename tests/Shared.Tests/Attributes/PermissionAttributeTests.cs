using Microsoft.VisualStudio.TestTools.UnitTesting;
using Diiwo.Identity.Shared.Attributes;
using Diiwo.Identity.Shared.Enums;
using System.Reflection;

namespace Shared.Tests.Attributes;

/// <summary>
/// Test suite for PermissionAttribute
/// Validates attribute functionality, property assignment, and reflection scenarios
/// </summary>
[TestClass]
public class PermissionAttributeTests
{
    /// <summary>
    /// Test Case: Basic Permission Attribute Creation
    /// Description: Verifies that PermissionAttribute can be created with minimal required parameters
    /// Acceptance Criteria:
    /// - Should accept action parameter and assign correctly
    /// - Should use default values for optional parameters
    /// - Should handle null action parameter appropriately
    /// </summary>
    [TestMethod]
    public void PermissionAttribute_BasicCreation_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var attribute = new PermissionAttribute("Read");

        // Assert
        Assert.AreEqual("Read", attribute.Action, "Action should be set correctly");
        Assert.IsNull(attribute.Description, "Description should be null by default");
        Assert.AreEqual(PermissionScope.Model, attribute.Scope, "Scope should default to Model");
        Assert.AreEqual(0, attribute.Priority, "Priority should default to 0");
    }

    /// <summary>
    /// Test Case: Permission Attribute with All Parameters
    /// Description: Verifies that PermissionAttribute can be created with all parameters specified
    /// Acceptance Criteria:
    /// - Should accept and assign all parameter values correctly
    /// - Should preserve custom scope and priority values
    /// - Should handle description parameter properly
    /// </summary>
    [TestMethod]
    public void PermissionAttribute_FullCreation_SetsAllPropertiesCorrectly()
    {
        // Arrange & Act
        var attribute = new PermissionAttribute(
            action: "ViewSensitive",
            description: "Access sensitive customer data", 
            scope: PermissionScope.Object,
            priority: 100
        );

        // Assert
        Assert.AreEqual("ViewSensitive", attribute.Action, "Action should be set correctly");
        Assert.AreEqual("Access sensitive customer data", attribute.Description, "Description should be set correctly");
        Assert.AreEqual(PermissionScope.Object, attribute.Scope, "Scope should be set correctly");
        Assert.AreEqual(100, attribute.Priority, "Priority should be set correctly");
    }

    /// <summary>
    /// Test Case: Null Action Parameter Handling
    /// Description: Verifies that PermissionAttribute throws appropriate exception for null action
    /// Acceptance Criteria:
    /// - Should throw ArgumentNullException when action is null
    /// - Should include parameter name in exception
    /// - Should not create attribute instance with null action
    /// </summary>
    [TestMethod]
    public void PermissionAttribute_NullAction_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() => 
            new PermissionAttribute(null!));
        
        Assert.AreEqual("action", exception.ParamName, "Exception should indicate the action parameter");
    }

    /// <summary>
    /// Test Case: Permission Scope Enumeration Values
    /// Description: Verifies that all PermissionScope values can be used with PermissionAttribute
    /// Acceptance Criteria:
    /// - Should accept Global scope
    /// - Should accept Model scope (default)
    /// - Should accept Object scope
    /// - Should preserve scope values correctly
    /// </summary>
    [TestMethod]
    public void PermissionAttribute_AllPermissionScopes_AreAcceptedCorrectly()
    {
        // Test Global scope
        var globalAttribute = new PermissionAttribute("Admin", scope: PermissionScope.Global);
        Assert.AreEqual(PermissionScope.Global, globalAttribute.Scope, "Should accept Global scope");

        // Test Model scope (default)
        var modelAttribute = new PermissionAttribute("Read", scope: PermissionScope.Model);
        Assert.AreEqual(PermissionScope.Model, modelAttribute.Scope, "Should accept Model scope");

        // Test Object scope
        var objectAttribute = new PermissionAttribute("ViewDetails", scope: PermissionScope.Object);
        Assert.AreEqual(PermissionScope.Object, objectAttribute.Scope, "Should accept Object scope");
    }

    /// <summary>
    /// Test Case: Priority Value Range Handling
    /// Description: Verifies that PermissionAttribute handles different priority values correctly
    /// Acceptance Criteria:
    /// - Should accept negative priority values
    /// - Should accept zero priority (default)
    /// - Should accept positive priority values
    /// - Should handle extreme values appropriately
    /// </summary>
    [TestMethod]
    public void PermissionAttribute_PriorityValues_AreHandledCorrectly()
    {
        // Test negative priority
        var negativeAttribute = new PermissionAttribute("Low", priority: -10);
        Assert.AreEqual(-10, negativeAttribute.Priority, "Should accept negative priority");

        // Test zero priority (default)
        var zeroAttribute = new PermissionAttribute("Default");
        Assert.AreEqual(0, zeroAttribute.Priority, "Should default to zero priority");

        // Test positive priority
        var positiveAttribute = new PermissionAttribute("High", priority: 100);
        Assert.AreEqual(100, positiveAttribute.Priority, "Should accept positive priority");

        // Test extreme values
        var extremeAttribute = new PermissionAttribute("Extreme", priority: int.MaxValue);
        Assert.AreEqual(int.MaxValue, extremeAttribute.Priority, "Should handle extreme priority values");
    }

    /// <summary>
    /// Test Case: Multiple Attributes on Single Class
    /// Description: Verifies that multiple PermissionAttribute instances can be applied to one class
    /// Acceptance Criteria:
    /// - Should allow multiple attributes on a single class
    /// - Should preserve all attribute instances
    /// - Should maintain correct property values for each attribute
    /// - Should support reflection-based retrieval
    /// </summary>
    [TestMethod]
    public void PermissionAttribute_MultipleAttributes_AreSupported()
    {
        // Act - Get attributes from test class using reflection
        var attributes = typeof(TestEntityWithMultiplePermissions)
            .GetCustomAttributes<PermissionAttribute>()
            .ToArray();

        // Assert
        Assert.AreEqual(4, attributes.Length, "Should have 4 permission attributes");

        // Verify each attribute
        var readAttribute = attributes.First(a => a.Action == "View");
        Assert.AreEqual("View customer data", readAttribute.Description);
        Assert.AreEqual(PermissionScope.Model, readAttribute.Scope);
        Assert.AreEqual(10, readAttribute.Priority);

        var createAttribute = attributes.First(a => a.Action == "Create");
        Assert.AreEqual("Create new customers", createAttribute.Description);
        Assert.AreEqual(PermissionScope.Global, createAttribute.Scope);
        Assert.AreEqual(20, createAttribute.Priority);

        var updateAttribute = attributes.First(a => a.Action == "Edit");
        Assert.AreEqual("Edit existing customers", updateAttribute.Description);
        Assert.AreEqual(PermissionScope.Object, updateAttribute.Scope);
        Assert.AreEqual(15, updateAttribute.Priority);

        var deleteAttribute = attributes.First(a => a.Action == "Delete");
        Assert.AreEqual("Delete customers", deleteAttribute.Description);
        Assert.AreEqual(PermissionScope.Object, deleteAttribute.Scope);
        Assert.AreEqual(50, deleteAttribute.Priority);
    }

    /// <summary>
    /// Test Case: Attribute Usage Validation
    /// Description: Verifies that PermissionAttribute has correct AttributeUsage settings
    /// Acceptance Criteria:
    /// - Should be applicable to classes only
    /// - Should allow multiple instances (AllowMultiple = true)
    /// - Should not be inherited by default
    /// </summary>
    [TestMethod]
    public void PermissionAttribute_AttributeUsage_IsConfiguredCorrectly()
    {
        // Arrange
        var attributeType = typeof(PermissionAttribute);
        var attributeUsage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        Assert.IsNotNull(attributeUsage, "PermissionAttribute should have AttributeUsage defined");
        Assert.AreEqual(AttributeTargets.Class, attributeUsage.ValidOn, 
            "Should be applicable to classes only");
        Assert.IsTrue(attributeUsage.AllowMultiple, 
            "Should allow multiple instances on the same class");
    }

    /// <summary>
    /// Test Case: Empty String Parameters Handling
    /// Description: Verifies how PermissionAttribute handles empty string parameters
    /// Acceptance Criteria:
    /// - Should accept empty action string (though not recommended)
    /// - Should accept empty description string
    /// - Should preserve empty strings rather than converting to null
    /// </summary>
    [TestMethod]
    public void PermissionAttribute_EmptyStrings_AreHandledCorrectly()
    {
        // Test empty action
        var emptyActionAttribute = new PermissionAttribute("");
        Assert.AreEqual("", emptyActionAttribute.Action, "Should accept empty action string");

        // Test empty description
        var emptyDescAttribute = new PermissionAttribute("Read", description: "");
        Assert.AreEqual("", emptyDescAttribute.Description, "Should accept empty description string");
    }

    /// <summary>
    /// Test Case: Whitespace Parameters Handling
    /// Description: Verifies how PermissionAttribute handles whitespace-only parameters
    /// Acceptance Criteria:
    /// - Should accept whitespace-only action (though not recommended)
    /// - Should accept whitespace-only description
    /// - Should preserve whitespace rather than trimming
    /// </summary>
    [TestMethod]
    public void PermissionAttribute_WhitespaceParameters_ArePreserved()
    {
        // Test whitespace action
        var whitespaceActionAttribute = new PermissionAttribute("   ");
        Assert.AreEqual("   ", whitespaceActionAttribute.Action, "Should preserve whitespace in action");

        // Test whitespace description  
        var whitespaceDescAttribute = new PermissionAttribute("Read", description: "   ");
        Assert.AreEqual("   ", whitespaceDescAttribute.Description, "Should preserve whitespace in description");
    }

    /// <summary>
    /// Test Case: Real-world Entity Examples
    /// Description: Verifies attribute works correctly with realistic entity examples
    /// Acceptance Criteria:
    /// - Should work with e-commerce entities
    /// - Should work with CMS entities
    /// - Should support domain-specific actions
    /// </summary>
    [TestMethod]
    public void PermissionAttribute_RealWorldExamples_WorkCorrectly()
    {
        // Test Product entity
        var productAttributes = typeof(TestProduct).GetCustomAttributes<PermissionAttribute>().ToArray();
        Assert.AreEqual(3, productAttributes.Length, "Product should have 3 permissions");
        Assert.IsTrue(productAttributes.Any(a => a.Action == "View"), "Should have View permission");
        Assert.IsTrue(productAttributes.Any(a => a.Action == "Create"), "Should have Create permission");
        Assert.IsTrue(productAttributes.Any(a => a.Action == "ManageInventory"), "Should have ManageInventory permission");

        // Test Article entity
        var articleAttributes = typeof(TestArticle).GetCustomAttributes<PermissionAttribute>().ToArray();
        Assert.AreEqual(2, articleAttributes.Length, "Article should have 2 permissions");
        Assert.IsTrue(articleAttributes.Any(a => a.Action == "Publish"), "Should have Publish permission");
        Assert.IsTrue(articleAttributes.Any(a => a.Action == "Moderate" && a.Scope == PermissionScope.Global), 
            "Should have Global Moderate permission");
    }
}

/// <summary>
/// Test entity class with multiple permission attributes
/// Used for testing reflection-based attribute retrieval
/// </summary>
[Permission("View", "View customer data", PermissionScope.Model, 10)]
[Permission("Create", "Create new customers", PermissionScope.Global, 20)]
[Permission("Edit", "Edit existing customers", PermissionScope.Object, 15)]
[Permission("Delete", "Delete customers", PermissionScope.Object, 50)]
internal class TestEntityWithMultiplePermissions
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Test product entity for real-world scenarios
/// </summary>
[Permission("View", "View products")]
[Permission("Create", "Add new products")]
[Permission("ManageInventory", "Manage stock levels", PermissionScope.Global)]
internal class TestProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

/// <summary>
/// Test article entity for CMS scenarios
/// </summary>
[Permission("Publish", "Publish articles")]
[Permission("Moderate", "Moderate content", PermissionScope.Global)]
internal class TestArticle
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}