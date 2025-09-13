using Diiwo.Identity.Shared.Extensions;
using Diiwo.Identity.Shared.Attributes;
using Diiwo.Identity.Shared.Enums;

namespace Usage;

/// <summary>
/// Simple usage example showing the minimal setup required
/// Just one line of code for automatic permission generation
/// </summary>
public class SimpleUsageExample
{
    /// <summary>
    /// Minimal Program.cs setup - works without any configuration
    /// Automatically detects Development environment and generates permissions
    /// </summary>
    public static async Task MinimalSetup()
    {
        var builder = WebApplication.CreateBuilder();
        
        // Add your DbContext as usual
        // builder.Services.AddDbContext<AppIdentityDbContext>(...);
        
        var app = builder.Build();
        
        // One line to generate all permissions automatically
        await app.GeneratePermissionsAsync();
        
        app.Run();
    }
    
    /// <summary>
    /// Setup with basic configuration override
    /// Forces permission generation regardless of environment
    /// </summary>
    public static async Task ConfiguredSetup()
    {
        var builder = WebApplication.CreateBuilder();
        
        // Enable detailed logging (optional)
        builder.Services.AddLogging();
        
        var app = builder.Build();
        
        // Generates permissions based on appsettings.json configuration
        await app.GeneratePermissionsAsync();
        
        app.Run();
    }
}

/// <summary>
/// Example entities that will automatically generate permissions
/// No additional setup required - just add the attributes
/// </summary>
[Permission("View", "View customer information")]
[Permission("Create", "Create new customers")]
[Permission("Edit", "Edit customer details")]
[Permission("Delete", "Delete customers")]
public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

[Permission("View", "View product catalog")]
[Permission("Create", "Add new products")]
[Permission("Edit", "Modify product information")]
[Permission("Manage", "Full product management", PermissionScope.Global)]
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

[Permission("View", "View order details")]
[Permission("Create", "Create new orders")]
[Permission("Cancel", "Cancel orders")]
[Permission("Process", "Process order fulfillment")]
[Permission("Refund", "Process order refunds", PermissionScope.Object, 50)]
public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Total { get; set; }
    public DateTime OrderDate { get; set; }
}