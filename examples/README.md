# 🚀 Permission System Examples

Complete examples for using the DIIWO Identity permission system with automatic generation from entity attributes.

## 📋 Table of Contents

- [Quick Start - Simplified Workflow](#-quick-start---simplified-workflow)
- [Advanced Workflow - Migrations](#-advanced-workflow---migrations)
- [Permission Attributes](#-permission-attributes)
- [Medical System Example](#-medical-system-example)
- [Configuration Examples](#-configuration-examples)
- [Best Practices](#-best-practices)

## ⚡ Quick Start - Simplified Workflow

**Recommended for development and small to medium projects**

### 1. Define Your Entity with Permissions

```csharp
using Diiwo.Identity.Shared.Attributes;

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
```

### 2. Set Up CLI Support in Program.cs

```csharp
using Diiwo.Identity.Shared.Extensions;

// Process CLI commands first
if (args.ProcessPermissionCommands())
{
    return; // Exit after processing command
}

// Continue with normal app startup
var builder = WebApplication.CreateBuilder(args);
// ... your normal app configuration ...
```

### 3. Apply Permissions Directly

```bash
# Apply permissions directly to database (Recommended)
dotnet run -- --apply-permissions

# Preview permissions without applying
dotnet run -- --make-permissions-preview

# Show help
dotnet run -- --permissions-help
```

## 🔧 Advanced Workflow - Migrations

**Recommended for production and enterprise environments**

### Migration-Based Approach

```bash
# Generate migration file
dotnet run -- --make-permissions

# Generate with custom name
dotnet run -- --make-permissions --name AddCustomerPermissions

# Apply migration
dotnet ef database update
```

See [Simplified Permission Workflow](../docs/SIMPLIFIED-PERMISSION-WORKFLOW.md) for detailed comparison.

## 🏷️ Permission Attributes

### Basic Permission

```csharp
[Permission("View", "View customer information")]
public class Customer { }
```

### Permission with Scope and Priority

```csharp
[Permission("Edit", "Edit customer details", PermissionScope.Object, 25)]
[Permission("Delete", "Delete customer records", PermissionScope.Global, 75)]
public class Customer { }
```

### Multiple Permissions

```csharp
[Permission("View", "View products")]
[Permission("Create", "Add new products")]
[Permission("ManageInventory", "Manage stock levels", PermissionScope.Global, 50)]
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

## 🏥 Medical System Example

Complete example demonstrating medical system permissions:

```csharp
[Permission("View", "View patient information")]
[Permission("Edit", "Edit patient details", PermissionScope.Object)]
[Permission("Create", "Create new patients")]
[Permission("ViewMedicalHistory", "View patient medical history", PermissionScope.Object, 30)]
public class Patient
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
}

[Permission("View", "View appointments")]
[Permission("Schedule", "Schedule appointments")]
[Permission("Cancel", "Cancel appointments", PermissionScope.Object, 40)]
[Permission("Reschedule", "Reschedule appointments", PermissionScope.Object, 35)]
public class Appointment
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string Notes { get; set; } = string.Empty;

    public Patient Patient { get; set; } = null!;
}
```

### Usage Example

```csharp
public class PermissionGenerationExample
{
    public static async Task RunExample(IServiceProvider services)
    {
        // Apply permissions for medical entities
        var permissionsCreated = await services.GeneratePermissionsAsync();

        Console.WriteLine($"Created {permissionsCreated} permissions for medical system");
    }
}
```

## ⚙️ Configuration Examples

### Development Configuration (appsettings.Development.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MedicalSystemDev;Trusted_Connection=true;"
  },
  "PermissionGeneration": {
    "Enabled": true,
    "EnableLogging": true
  }
}
```

### Production Configuration (appsettings.Production.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server;Database=MedicalSystem;User Id=app;Password=***;"
  },
  "PermissionGeneration": {
    "Enabled": false,
    "EnableLogging": false
  }
}
```

## 🎯 Best Practices

### 1. Permission Naming

- Use consistent action verbs: `View`, `Create`, `Edit`, `Delete`
- Use descriptive names for special permissions: `ViewMedicalHistory`, `ManageInventory`
- Keep permission names concise but clear

### 2. Scope and Priority

- Use `PermissionScope.Object` for instance-specific permissions
- Use `PermissionScope.Global` for system-wide permissions
- Lower priority numbers = higher importance (0 = highest)

### 3. Development Workflow

```bash
# Development (fast iteration)
dotnet run -- --apply-permissions

# Production (controlled deployment)
dotnet run -- --make-permissions
dotnet ef database update
```

### 4. Architecture Selection

- **Simplified workflow**: Development, prototyping, small projects
- **Migration workflow**: Production, enterprise, code review requirements

## 🚨 Troubleshooting

### Common Issues

**"No database connection string found"**
```bash
Solution: Add ConnectionStrings section to appsettings.json
```

**"No supported DbContext found"**
```bash
Solution: Ensure AppIdentityDbContext or AspNetIdentityDbContext is registered
```

**Permission conflicts**
```bash
System automatically handles duplicates - safe to re-run
```

## 📚 Related Documentation

- [Simplified Permission Workflow](../docs/SIMPLIFIED-PERMISSION-WORKFLOW.md) - Complete workflow guide
- [Repository README](../README.md) - Project overview and dual architecture information

---

*These examples demonstrate both simplified and advanced workflows - choose the approach that best fits your development needs.*