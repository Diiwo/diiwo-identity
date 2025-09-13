using Diiwo.Identity.Shared.Extensions;
using Diiwo.Identity.Shared.Attributes;
using Diiwo.Identity.Shared.Enums;

namespace Usage;

/// <summary>
/// Advanced usage examples showing optional configuration scenarios
/// Demonstrates how to customize permission generation behavior
/// </summary>
public class AdvancedUsageExample
{
    /// <summary>
    /// Multi-assembly permission generation
    /// Scans multiple assemblies for entities with PermissionAttribute
    /// </summary>
    public static async Task MultiAssemblySetup()
    {
        var builder = WebApplication.CreateBuilder();
        
        // Configure to scan multiple assemblies
        builder.Configuration["PermissionGeneration:ScanAssemblies:0"] = "MyApp.Core";
        builder.Configuration["PermissionGeneration:ScanAssemblies:1"] = "MyApp.Medical";
        builder.Configuration["PermissionGeneration:ScanAssemblies:2"] = "MyApp.Business";
        builder.Configuration["PermissionGeneration:EnableLogging"] = "true";
        
        var app = builder.Build();
        
        await app.GeneratePermissionsAsync();
        
        app.Run();
    }
    
    /// <summary>
    /// Production-safe permission generation
    /// Only generates permissions when explicitly enabled
    /// </summary>
    public static async Task ProductionSafeSetup()
    {
        var builder = WebApplication.CreateBuilder();
        
        var app = builder.Build();
        
        // In production, this will only run if explicitly enabled in config
        // appsettings.Production.json: { "PermissionGeneration": { "Enabled": true } }
        await app.GeneratePermissionsAsync();
        
        app.Run();
    }
    
    /// <summary>
    /// Skip generation if permissions exist
    /// Useful for avoiding regeneration in staging environments
    /// </summary>
    public static async Task ConditionalGeneration()
    {
        var builder = WebApplication.CreateBuilder();
        
        // Configure to skip if any permissions already exist
        builder.Configuration["PermissionGeneration:SkipIfPermissionsExist"] = "true";
        builder.Configuration["PermissionGeneration:EnableLogging"] = "true";
        
        var app = builder.Build();
        
        await app.GeneratePermissionsAsync();
        
        app.Run();
    }
    
    /// <summary>
    /// Custom table name override
    /// Useful when you need to use different permission table names
    /// </summary>
    public static async Task CustomTableSetup()
    {
        var builder = WebApplication.CreateBuilder();
        
        // Use custom table name instead of architecture defaults
        builder.Configuration["PermissionGeneration:TableName"] = "Custom_Permissions";
        
        var app = builder.Build();
        
        await app.GeneratePermissionsAsync();
        
        app.Run();
    }
}

/// <summary>
/// Complex domain entity example with varied permission scopes and priorities
/// Shows how to model real-world business permissions
/// </summary>
[Permission("View", "View basic employee information", PermissionScope.Model)]
[Permission("ViewSalary", "View employee salary information", PermissionScope.Object, 100)]
[Permission("ViewPersonal", "View personal employee details", PermissionScope.Object, 75)]
[Permission("Create", "Create new employee records", PermissionScope.Global, 50)]
[Permission("Edit", "Edit employee information", PermissionScope.Object, 25)]
[Permission("Terminate", "Terminate employee", PermissionScope.Object, 200)]
[Permission("ManagePayroll", "Manage employee payroll", PermissionScope.Global, 150)]
public class Employee
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Department entity with hierarchical permissions
/// Demonstrates organizational permission modeling
/// </summary>
[Permission("View", "View department information", PermissionScope.Model)]
[Permission("Manage", "Manage department settings", PermissionScope.Object)]
[Permission("ViewReports", "View department reports", PermissionScope.Object, 25)]
[Permission("ManageBudget", "Manage department budget", PermissionScope.Object, 100)]
[Permission("CreateDepartment", "Create new departments", PermissionScope.Global, 75)]
public class Department
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ParentDepartmentId { get; set; }
    public decimal Budget { get; set; }
    public Guid ManagerId { get; set; }
}

/// <summary>
/// Project entity with time-sensitive permissions
/// Shows how to model project-based access control
/// </summary>
[Permission("View", "View project details", PermissionScope.Object)]
[Permission("Edit", "Edit project information", PermissionScope.Object, 25)]
[Permission("AssignMembers", "Assign team members to project", PermissionScope.Object, 50)]
[Permission("ViewFinancials", "View project financial information", PermissionScope.Object, 100)]
[Permission("Approve", "Approve project milestones", PermissionScope.Object, 75)]
[Permission("Archive", "Archive completed projects", PermissionScope.Object, 150)]
[Permission("CreateProject", "Create new projects", PermissionScope.Global, 200)]
public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Budget { get; set; }
    public Guid ProjectManagerId { get; set; }
    public string Status { get; set; } = "Active";
}