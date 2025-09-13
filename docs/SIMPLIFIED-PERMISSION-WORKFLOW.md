# 🚀 Simplified Permission Workflow

**One-command permission application for rapid development**

*Complementary to the [Automatic Migrations](../examples/AutomaticMigrations.md) system*

## ⚡ Quick Overview

The simplified workflow allows applying permissions directly to the database with a single command, bypassing the migration file generation process entirely.

### Before vs After

| Traditional Workflow | Simplified Workflow |
|---------------------|-------------------|
| 1. Add `[Permission]` attributes | 1. Add `[Permission]` attributes |
| 2. `dotnet run -- --make-permissions` | 2. `dotnet run -- --apply-permissions` |
| 3. Edit generated migration file | 3. ✅ **Done!** |
| 4. `dotnet ef database update` | |

## 🛠️ Commands

### Primary Command (Recommended for Development)
```bash
dotnet run -- --apply-permissions
```

**What it does:**
- Auto-detects App/AspNet architecture
- Scans for entities with `[Permission]` attributes
- Shows preview of permissions to be created
- Applies permissions directly to database
- Prevents duplicates automatically

### Support Commands
```bash
# Preview without applying
dotnet run -- --make-permissions-preview

# Show help
dotnet run -- --permissions-help

# Traditional migration workflow (still available)
dotnet run -- --make-permissions
```

## 🔧 Setup Requirements

### 1. Configure Connection String
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyApp;Trusted_Connection=true;"
  }
}
```

### 2. Program.cs Integration
```csharp
using Diiwo.Identity.Shared.Extensions;

public static async Task Main(string[] args)
{
    // Process CLI commands before app startup
    if (args.ProcessPermissionCommands())
    {
        return; // Exit after command processing
    }

    // Continue with normal application startup
    var builder = WebApplication.CreateBuilder(args);
    // ... rest of app configuration
}
```

## 📋 Example Workflow

### 1. Define Entities with Permissions
```csharp
[Permission("View", "View customer information")]
[Permission("Edit", "Edit customer details", PermissionScope.Object, 25)]
[Permission("Delete", "Delete customer records", PermissionScope.Global, 75)]
public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // ... more properties
}
```

### 2. Apply Permissions
```bash
dotnet run -- --apply-permissions
```

### 3. Example Output
```
🚀 Direct Permission Application
================================

📋 Scanning for entities with [Permission] attributes...

🔍 Permission Preview:
═══════════════════════

📋 Customer (3 permissions):
  ✅ View: View customer information
  ✅ Edit: Edit customer details (Scope: Object, Priority: 25)
  ✅ Delete: Delete customer records (Scope: Global, Priority: 75)

📊 Summary: 3 total permissions across 1 entities

🤔 Apply these permissions to database? (y/N): y

🔍 Auto-detecting architecture and applying permissions...
📱 Detected App architecture (AppIdentityDbContext)
✅ Successfully applied 3 permissions to database!
🎉 All done! Your permissions are now available in the database.
```

## 🎯 When to Use Each Approach

### Simplified Workflow (`--apply-permissions`)
✅ **Best for:**
- Rapid development
- Prototyping
- Testing environments
- Small to medium projects
- When you need immediate results

### Migration Workflow (`--make-permissions`)
✅ **Best for:**
- Production deployments
- Enterprise environments
- Code review requirements
- Migration history tracking
- Complex deployment pipelines

## 🔧 Technical Details

### Architecture Auto-Detection
The system automatically detects your architecture:

```csharp
// Tries App architecture first
var appContext = services.GetService<AppIdentityDbContext>();
if (appContext != null) {
    // Uses App permission generation
}

// Falls back to AspNet architecture
var aspNetContext = services.GetService<AspNetIdentityDbContext>();
if (aspNetContext != null) {
    // Uses AspNet permission generation
}
```

### Connection String Discovery
Searches for connection strings in this order:
1. `DefaultConnection`
2. `Database`
3. `Identity`
4. `SqlServer`
5. `Main`

### Duplicate Prevention
- Checks existing database records
- Verifies current transaction state
- Skips duplicates automatically

## 🚨 Error Handling

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

## 🔄 Migration Path

You can switch between workflows at any time:

```bash
# Start with simplified approach
dotnet run -- --apply-permissions

# Later switch to migrations for production
dotnet run -- --make-permissions
dotnet ef database update
```

Both approaches create identical database records.

## 🧪 Testing

The simplified permission system includes comprehensive test coverage:

### Test Suites (All Passing ✅)
- **PermissionCommandsTests** (10/10) - CLI command processing and file generation
- **CliExtensionsTests** (10/10) - Program.cs integration and argument handling
- **PermissionGenerationIntegrationTests** (6/6) - End-to-end permission generation

### Running Tests
```bash
# All tests
dotnet test

# Specific test suites
dotnet test --filter "PermissionCommandsTests"
dotnet test --filter "CliExtensionsTests"
```

## 📚 Related Documentation

- [Automatic Migrations Guide](../examples/AutomaticMigrations.md) - Traditional workflow
- [App Architecture Guide](./APP-IMPLEMENTATION-GUIDE.md) - Core implementation
- [Permission Attributes](../examples/AutomaticMigrations.md#permission-attributes) - Attribute usage

---

*This simplified workflow complements the existing migration system - use the approach that best fits your development needs.*