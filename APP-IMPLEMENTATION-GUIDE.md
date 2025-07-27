# 🔧 App Architecture Implementation Guide

**Complete implementation guide for the DIIWO Identity App Architecture**

*Based on current implementation as of v0.1.0*

## 📋 Table of Contents

- [Overview](#overview)
- [Entity Model](#entity-model)
- [Database Context](#database-context)
- [Service Layer](#service-layer)
- [Permission System](#permission-system)
- [Testing](#testing)
- [Usage Examples](#usage-examples)

## 🎯 Overview

The App Architecture provides a complete, standalone identity management system without dependencies on ASP.NET Core Identity. This implementation is fully functional and tested.

### ✅ What's Currently Available

- **Complete entity model** with relationships
- **Database context** with proper seeding
- **Service layer** for user and permission management
- **5-level permission system** foundation
- **Comprehensive test suite** with full coverage

## 🗃️ Entity Model

### AppUser - Core User Entity

```csharp
public class AppUser : UserTrackedEntity
{
    // Basic Properties
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    
    // Authentication Properties
    public bool EmailConfirmed { get; set; } = false;
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    
    // Computed Properties
    public string FullName => $"{FirstName} {LastName}".Trim();
    
    // Navigation Properties
    public virtual ICollection<AppUserSession> UserSessions { get; set; }
    public virtual ICollection<AppUserLoginHistory> LoginHistory { get; set; }
    public virtual ICollection<AppUserPermission> UserPermissions { get; set; }
    public virtual ICollection<AppGroup> UserGroups { get; set; }
}
```

**Key Features:**
- Inherits from `UserTrackedEntity` (from DIIWO-Core)
- Built-in lockout mechanism
- Email confirmation workflow
- Rich navigation properties for relationships

### AppRole - Role Management

```csharp
public class AppRole : UserTrackedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Navigation Properties
    public virtual ICollection<AppRolePermission> RolePermissions { get; set; }
}
```

**Default Roles (Seeded):**
- **SuperAdmin** - Full system access
- **Admin** - Administrative privileges
- **User** - Basic user permissions

### AppGroup - Group-based Access

```csharp
public class AppGroup : UserTrackedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Navigation Properties
    public virtual ICollection<AppGroupPermission> GroupPermissions { get; set; }
    
    // Computed Method
    public IEnumerable<AppPermission> GetGrantedPermissions() => 
        GroupPermissions.Where(gp => gp.IsGranted).Select(gp => gp.Permission);
}
```

### AppPermission - Permission Definition

```csharp
public class AppPermission : UserTrackedEntity
{
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public PermissionScope Scope { get; set; } = PermissionScope.Global;
    public string? Description { get; set; }
}
```

**Default Permissions (Seeded):**
- **Users:Read** - View user information
- **Users:Write** - Create/modify users
- **Roles:Manage** - Manage roles and assignments
- **System:Admin** - System administration
- **Reports:View** - Access to reports

### Permission Junction Tables

All permission relationships use priority-based evaluation:

```csharp
public abstract class PermissionBase : UserTrackedEntity
{
    public Guid PermissionId { get; set; }
    public bool IsGranted { get; set; } = true;
    public int Priority { get; set; } = 100;
    
    public virtual AppPermission Permission { get; set; } = null!;
}

// Specific implementations:
// - AppRolePermission (Priority: 0 - Highest)
// - AppGroupPermission (Priority: 50)
// - AppUserPermission (Priority: 100)
```

## 🗄️ Database Context

### AppIdentityDbContext

Complete Entity Framework context with proper configuration:

```csharp
public class AppIdentityDbContext : DbContext
{
    // DbSets
    public DbSet<AppUser> Users { get; set; }
    public DbSet<AppRole> Roles { get; set; }
    public DbSet<AppGroup> Groups { get; set; }
    public DbSet<AppPermission> Permissions { get; set; }
    public DbSet<AppUserSession> UserSessions { get; set; }
    public DbSet<AppUserLoginHistory> LoginHistory { get; set; }
    
    // Permission junction tables
    public DbSet<AppRolePermission> RolePermissions { get; set; }
    public DbSet<AppGroupPermission> GroupPermissions { get; set; }
    public DbSet<AppUserPermission> UserPermissions { get; set; }
}
```

### Database Seeding

The context automatically seeds:

1. **Default Roles**: SuperAdmin, Admin, User
2. **Default Permissions**: System operations
3. **Role-Permission Mappings**: SuperAdmin gets all permissions
4. **Proper Foreign Key Relationships**

### Usage Example

```csharp
var options = new DbContextOptionsBuilder<AppIdentityDbContext>()
    .UseSqlServer(connectionString)
    .Options;

using var context = new AppIdentityDbContext(options);
await context.Database.EnsureCreatedAsync();
```

## ⚙️ Service Layer

### AppUserService - User Management

Complete CRUD operations for users:

```csharp
public interface IAppUserService
{
    Task<AppUser?> GetUserByIdAsync(Guid userId);
    Task<AppUser?> GetUserByEmailAsync(string email);
    Task<AppUser> CreateUserAsync(string email, string passwordHash, string firstName, string lastName);
    Task<bool> UpdateUserAsync(AppUser user);
    Task<bool> DeleteUserAsync(Guid userId);
}
```

**Key Methods:**
- **GetUserByIdAsync** - Retrieve user by unique identifier
- **GetUserByEmailAsync** - Find user by email address
- **CreateUserAsync** - Create new user with validation
- **UpdateUserAsync** - Modify existing user information
- **DeleteUserAsync** - Remove user from system

### AppPermissionService - Permission Management

Advanced permission checking with 5-level hierarchy:

```csharp
public interface IAppPermissionService
{
    Task<bool> UserHasPermissionAsync(Guid userId, string resource, string action);
    Task<bool> GrantUserPermissionAsync(Guid userId, Guid permissionId, int priority = 100);
    Task<bool> RevokeUserPermissionAsync(Guid userId, Guid permissionId);
    Task<IEnumerable<AppPermission>> GetUserPermissionsAsync(Guid userId);
}
```

**Permission Evaluation Logic:**
1. Check Role permissions (Priority 0 - Highest)
2. Check Group permissions (Priority 50)
3. Check User permissions (Priority 100)
4. **DENY always overrides GRANT**
5. **Higher priority (lower number) wins**

### Service Registration Example

```csharp
// Program.cs or Startup.cs
services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddScoped<IAppUserService, AppUserService>();
services.AddScoped<IAppPermissionService, AppPermissionService>();
```

## 🔐 Permission System Implementation

### 5-Level Permission Hierarchy

Currently implemented levels:

1. **🏆 Role Permissions** (Priority 0) - ✅ **Implemented**
2. **👥 Group Permissions** (Priority 50) - ✅ **Implemented** 
3. **👤 User Permissions** (Priority 100) - ✅ **Implemented**
4. **📊 Model Permissions** (Priority 150) - *Planned*
5. **🎯 Object Permissions** (Priority 200) - *Planned*

### Permission Checking Example

```csharp
// Check if user can read documents
var hasPermission = await _permissionService.UserHasPermissionAsync(
    userId, 
    "Documents", 
    "Read"
);

if (hasPermission)
{
    // Allow access to documents
    var documents = await GetUserDocuments(userId);
}
```

### Grant Permission Example

```csharp
// Grant user permission to write documents
await _permissionService.GrantUserPermissionAsync(
    userId, 
    documentWritePermissionId, 
    priority: 100
);
```

## 🧪 Testing Implementation

### Comprehensive Test Coverage

**Entity Tests** (`AppUserTests`, `AppPermissionTests`, etc.):
```csharp
[TestMethod]
public void Constructor_SetsDefaultValues()
{
    // Test Case: AppUser Constructor Initialization
    // Description: Verifies that AppUser constructor sets all default values correctly
    // Acceptance Criteria:
    // - User ID should be automatically generated and not empty
    // - IsActive should default to true for new users
    // - EmailConfirmed should default to false requiring email verification
    
    var user = new AppUser
    {
        Email = "test@example.com",
        PasswordHash = "hashed-password"
    };
    
    Assert.AreNotEqual(Guid.Empty, user.Id, "User ID should be automatically generated");
    Assert.IsTrue(user.IsActive, "New users should be active by default");
    Assert.IsFalse(user.EmailConfirmed, "Email should require confirmation by default");
}
```

**Integration Tests** (`AppIdentityDbContextTests`):
```csharp
[TestMethod]
public async Task CanCreateAndRetrieveUser()
{
    // Test Case: Database User CRUD Operations
    // Description: Verifies that users can be created, saved, and retrieved from the database
    
    var user = new AppUser
    {
        Email = "integration@example.com",
        PasswordHash = "hashed-password",
        FirstName = "Integration",
        LastName = "Test"
    };
    
    _context.Users.Add(user);
    await _context.SaveChangesAsync();
    
    var retrievedUser = await _context.Users.FindAsync(user.Id);
    
    Assert.IsNotNull(retrievedUser, "User should be successfully retrieved from database");
    Assert.AreEqual("integration@example.com", retrievedUser.Email, "Email should be correctly persisted");
}
```

**Service Tests** (`AppUserServiceTests`, `AppPermissionServiceTests`):
```csharp
[TestMethod]
public async Task UserHasPermissionAsync_WithDirectUserPermission_ReturnsTrue()
{
    // Test Case: Direct User Permission Authorization
    // Description: Verifies that users with direct permissions are correctly authorized
    
    // ... arrange user and permission ...
    
    var result = await _permissionService.UserHasPermissionAsync(user.Id, "Document", "Read");
    
    Assert.IsTrue(result, "User should have permission when directly granted");
}
```

### Test Execution

```bash
# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter Category=Entity
dotnet test --filter Category=Integration  
dotnet test --filter Category=Service
```

## 💻 Usage Examples

### Complete Implementation Example

```csharp
public class UserController : ControllerBase
{
    private readonly IAppUserService _userService;
    private readonly IAppPermissionService _permissionService;
    private readonly AppIdentityDbContext _context;
    
    public UserController(
        IAppUserService userService,
        IAppPermissionService permissionService,
        AppIdentityDbContext context)
    {
        _userService = userService;
        _permissionService = permissionService;
        _context = context;
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        // Check permissions
        var hasPermission = await _permissionService.UserHasPermissionAsync(
            GetCurrentUserId(), "Users", "Read");
            
        if (!hasPermission)
            return Forbid();
        
        // Get user
        var user = await _userService.GetUserByIdAsync(id);
        return user != null ? Ok(user) : NotFound();
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // Check permissions
        var hasPermission = await _permissionService.UserHasPermissionAsync(
            GetCurrentUserId(), "Users", "Write");
            
        if (!hasPermission)
            return Forbid();
        
        // Create user
        var user = await _userService.CreateUserAsync(
            request.Email,
            HashPassword(request.Password),
            request.FirstName,
            request.LastName
        );
        
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }
}
```

### Database Setup Example

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Database
        services.AddDbContext<AppIdentityDbContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
        
        // Services
        services.AddScoped<IAppUserService, AppUserService>();
        services.AddScoped<IAppPermissionService, AppPermissionService>();
        
        // Logging
        services.AddLogging();
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Ensure database is created
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
        context.Database.EnsureCreated();
    }
}
```

## 📊 Current Limitations & Next Steps

### ✅ What Works Now
- Complete CRUD operations for users
- Basic permission checking (Role, Group, User levels)
- Database context with seeding
- Full test coverage

### 🚧 In Development
- **Model Permissions** (Priority 150)
- **Object Permissions** (Priority 200)
- **Advanced permission scoping**
- **Caching layer for permissions**

### 📋 Planned Features
- **Migration tools** between architectures
- **Password policies** and validation
- **Session management** improvements
- **Audit logging** enhancements

---

## 🔗 Related Documentation

- [README.md](README.md) - Project overview and quick start
- [LICENSE](LICENSE) - MIT License details
- **Test Documentation** - See test files for detailed implementation examples