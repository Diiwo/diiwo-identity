# 🎯 DIIWO Identity Solution

**Dual-architecture identity management library for modern .NET applications**

[![CI - Build and Test](https://github.com/Diiwo/diiwo-identity/actions/workflows/ci.yml/badge.svg)](https://github.com/Diiwo/diiwo-identity/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![NuGet](https://img.shields.io/nuget/v/Diiwo.Identity.svg)](https://www.nuget.org/packages/Diiwo.Identity)
[![GitHub](https://img.shields.io/badge/GitHub-diiwo%2Fdiiwo--identity-lightgrey.svg)](https://github.com/diiwo/diiwo-identity)

## 🏗️ Dual Architecture Design

This library provides two distinct architectures to choose from based on your project needs:

### 🎪 **App Architecture** - Simple & Standalone
Perfect for lightweight applications and microservices.

- ✅ **No ASP.NET Core Identity dependencies** 
- ✅ **Optimized database schema** without Identity overhead
- ✅ **Full control** over authentication and authorization
- ✅ **Ideal for**: APIs, microservices, console applications
- 🗃️ **Entities**: `AppUser`, `AppRole`, `AppGroup`, `AppPermission`

### 🏢 **AspNet Architecture** - Enterprise Ready ✅
Built on ASP.NET Core Identity with enterprise extensions.

- ✅ **Full ASP.NET Core Identity integration**
- ✅ **Compatible** with `UserManager<T>`, `RoleManager<T>`, `SignInManager<T>`
- ✅ **Enterprise features** on top of standard Identity
- ✅ **Ideal for**: Web applications, enterprise systems
- 🗃️ **Entities**: `IdentityUser`, `IdentityRole`, `IdentityGroup`, `IdentityPermission`

## ⚡ Current Implementation Status

### ✅ Recently Completed Features

- **🏗️ Dual Architecture Implementation**
  - **App Architecture**: Complete standalone implementation (`AppUser`, `AppRole`, `AppGroup`, `AppPermission`)
  - **AspNet Architecture**: Full ASP.NET Core Identity integration (`IdentityUser`, `IdentityRole`, `IdentityGroup`, `IdentityPermission`)
  - Database contexts with seeding for both architectures
  - Service layers with complete business logic

- **🔍 Enterprise Audit Trail System** ⭐ NEW
  - **Automatic audit tracking** - `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` fields managed automatically
  - **Soft delete support** - Entities marked as `Terminated` instead of hard deletion
  - **Entity state management** - Track entity lifecycle with `EntityState` enum
  - **Zero manual code** - All audit trails handled by AuditInterceptor from Diiwo.Core
  - **Compliance ready** - Enterprise-grade audit capabilities for regulatory requirements
  - **Complete examples** - Comprehensive demonstrations in `/examples` directory

- **🚀 Enterprise Session Management** ⭐ NEW
  - **Advanced session tracking** - Device fingerprinting, location tracking, SSO support
  - **JWT refresh tokens** - Secure token-based authentication with automatic refresh
  - **Session security** - IP tracking, user agent validation, concurrent session limits
  - **Enterprise features** - SSO provider integration, device management
  - **Complete audit trail** - All session activities automatically tracked

- **🚀 Advanced Permission System**
  - 5-level permission hierarchy with priority-based evaluation
  - Automatic permission generation from entity attributes
  - Simplified workflow for direct database application
  - Traditional migration workflow for enterprise deployment
  - CLI commands for streamlined development
  - **Full audit trail** for all permission changes

- **🧪 Comprehensive Test Suite**
  - Entity tests for both architectures with detailed documentation
  - Integration tests for database operations and relationships
  - Service tests with complete business logic validation
  - CLI tests for permission generation workflows
  - All tests include detailed assert comments for clarity

- **📦 Modern Project Structure**
  - Organized `src/` and `tests/` directory structure
  - Separate projects for App, AspNet, Shared, and Migration components
  - Solution file with proper project references
  - **Integration with Diiwo.Core** for base entities and automatic auditing

- **📖 Comprehensive Documentation** ✅
  - Complete architecture comparison guide (App vs AspNet)
  - Enterprise implementation examples and patterns
  - Production deployment strategies and best practices
  - Migration guides for architecture transitions

### 🚧 Currently In Development

- **🔄 Migration Services** - Architecture conversion utilities
  - App ↔ AspNet migration services
  - Data migration between architectures
  - Validation and rollback capabilities

### 📋 Planned Features

- **🌐 Multi-database support** (PostgreSQL, SQLite, MySQL)
- **📦 NuGet package publishing** and distribution
- **🔧 Enhanced migration tools** with validation and rollback
- **🎯 Performance optimizations** and caching strategies
- **🔒 Advanced security features** (MFA, risk-based authentication)
- **📊 Analytics dashboard** for user behavior and security monitoring

## 🔍 Enterprise Audit Trail System

**Zero-code automatic audit tracking** powered by Diiwo.Core AuditInterceptor:

### ✨ Key Features
- **🎯 Automatic Tracking**: `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` - **no manual code required**
- **🗑️ Soft Delete**: Entities marked as `Terminated` instead of permanent deletion
- **📊 State Management**: Track entity lifecycle with `EntityState` enum (`Active`, `Inactive`, `Terminated`)
- **👤 User Attribution**: Automatic tracking of who made changes and when
- **🏢 Compliance Ready**: Enterprise-grade audit capabilities for regulatory requirements

### 🎪 App Architecture Implementation
```csharp
// App entities inherit from DomainEntity - automatic audit trail!
public class AppUser : DomainEntity  // ✅ Inherits all audit capabilities
{
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    // CreatedAt, UpdatedAt, CreatedBy, UpdatedBy automatically managed!
}
```

### 🏢 AspNet Architecture Implementation
```csharp
// AspNet entities implement IDomainEntity - automatic audit trail!
public class IdentityUser : IdentityUser<Guid>, IDomainEntity  // ✅ Enterprise audit
{
    public string? FirstName { get; set; }
    // Audit fields (CreatedAt, UpdatedAt, etc.) automatically managed!
}
```

### 📝 Service Layer - No Manual Audit Code!
```csharp
// Before: Manual audit assignments ❌
var user = new AppUser
{
    Email = "user@example.com",
    CreatedAt = DateTime.UtcNow,        // ❌ Manual
    UpdatedAt = DateTime.UtcNow,        // ❌ Manual
    CreatedBy = currentUserId           // ❌ Manual
};

// After: Automatic audit tracking ✅
var user = new AppUser
{
    Email = "user@example.com"
    // ✅ CreatedAt, UpdatedAt, CreatedBy, UpdatedBy set automatically!
};
```

## 🎯 5-Level Permission System

Advanced permission hierarchy with priority-based evaluation:

```
1. 🏆 Role Permissions     (Priority 0 - HIGHEST)
2. 👥 Group Permissions    (Priority 50)
3. 👤 User Permissions     (Priority 100)
4. 📊 Model Permissions    (Priority 150)
5. 🎯 Object Permissions   (Priority 200 - LOWEST)
```

### Permission Evaluation Logic:
- ❌ **DENY always wins** over GRANT
- 🏆 **Higher priority** (lower number) takes precedence
- 🔒 **Deny by default** if no explicit permissions exist
- **🔍 Full audit trail** for all permission changes

## 🔧 Dependencies

This project depends on:
- **[Diiwo.Core](https://github.com/diiwo/diiwo-core)** - Base entities and shared functionality
- **.NET 8.0** - Latest .NET framework
- **Entity Framework Core** - Data access and ORM

## 🚀 Quick Start

### Installation

```bash
# Clone the repository
git clone https://github.com/diiwo/diiwo-identity.git

# Navigate to project directory
cd diiwo-identity

# Restore dependencies (including Diiwo.Core)
dotnet restore

# Run tests to verify installation
dotnet test
```

### Current App Architecture Usage (with Enterprise Audit)

```csharp
// ✅ Enterprise-ready entity with automatic audit trail
var user = new AppUser
{
    Email = "user@example.com",
    PasswordHash = "hashed-password",
    FirstName = "John",
    LastName = "Doe"
    // ✅ CreatedAt, UpdatedAt, CreatedBy, UpdatedBy automatically set!
};

// Permission checking with audit trail
var hasPermission = await _permissionService.UserHasPermissionAsync(userId, "Documents", "Read");

// User management with automatic audit tracking
var newUser = await _userService.CreateUserAsync("user@example.com", hashedPassword, "John", "Doe");
// ✅ All changes automatically tracked with full audit trail!

// Soft delete - preserves audit history
await _userService.DeleteUserAsync(userId);
// ✅ User marked as 'Terminated', not permanently deleted
```

### Enterprise AspNet Architecture Usage

```csharp
// ✅ ASP.NET Core Identity + Enterprise audit trail
var identityUser = new IdentityUser
{
    Email = "enterprise@example.com",
    UserName = "enterprise-user",
    FirstName = "Enterprise",
    LastName = "User"
    // ✅ IDomainEntity interface provides automatic audit trail!
};

// Full ASP.NET Core Identity integration with audit
var result = await _userManager.CreateAsync(identityUser, "SecurePassword123!");
// ✅ All Identity operations tracked with enterprise audit trail!

// Advanced permission system with audit
await _permissionService.AssignPermissionToUserAsync(userId, permissionId, isGranted: true);
// ✅ Permission changes tracked automatically!
```

### Permission Management

**🚀 Simplified Workflow** (Recommended for Development):
```bash
# Add [Permission] attributes to entities, then:
dotnet run -- --apply-permissions
```

**🔧 Advanced Workflow** (Enterprise/Production):
```bash
# Generate migration files:
dotnet run -- --make-permissions
dotnet ef database update
```

## 📚 Comprehensive Examples

Explore the complete enterprise features with our detailed examples:

### 🔍 Audit Trail Examples
```bash
# Run App Architecture audit trail examples
cd examples
dotnet run AuditTrailExample.cs

# Run AspNet Architecture enterprise examples
cd examples
dotnet run AspNetAuditTrailExample.cs
```

**Examples demonstrate**:
- ✅ **User Lifecycle Tracking** - Create, update, soft delete with automatic audit
- ✅ **Permission Management** - 5-level permission system with full audit trail
- ✅ **Session Management** - Complete session lifecycle tracking
- ✅ **Login History** - Authentication attempt logging with enterprise audit
- ✅ **Group Management** - User organization with permission inheritance
- ✅ **Enterprise Integration** - ASP.NET Core Identity with advanced audit features

See [examples/README.md](examples/README.md) for detailed documentation and sample output.

📖 **Documentation:**
- [Architecture Comparison Guide](docs/ARCHITECTURE-COMPARISON.md) - Choose between App vs AspNet architectures
- [Implementation Examples](docs/IMPLEMENTATION-EXAMPLES.md) - Enterprise patterns and code examples
- [Simplified Workflow Guide](docs/SIMPLIFIED-PERMISSION-WORKFLOW.md) - One-command approach
- [Examples and Usage](examples/README.md) - Complete examples and best practices

## 🧪 Testing

The project includes comprehensive test coverage across all implemented features:

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test projects
dotnet test tests/App.Tests/ --filter Category=Entity
dotnet test tests/AspNet.Tests/ --filter Category=Integration
dotnet test tests/Shared.Tests/ --filter Category=Service
```

**Current Test Structure:**
- **App.Tests**: App architecture entity, integration, and service tests
- **AspNet.Tests**: AspNet architecture with Identity integration tests
- **Shared.Tests**: CLI commands, permission generation, and shared component tests

## 📖 Architecture Decision

### When to choose App Architecture:
- 🎯 **Simple applications** with basic user management needs
- 🚀 **Microservices** that need lightweight identity
- 🎮 **Console applications** or background services
- 🔧 **Custom authentication** requirements
- ⚡ **Performance-critical** applications requiring minimal overhead
- 🔄 **High flexibility** for custom business logic

### When to choose AspNet Architecture:
- 🏢 **Enterprise web applications**
- 🔐 **Complex authentication** scenarios (2FA, OAuth, etc.)
- 🌐 **Web applications** using ASP.NET Core Identity features
- 🎭 **Role-based and policy-based** authorization
- ⚙️ **Integration** with existing ASP.NET Core Identity systems
- 🛡️ **Advanced security** requirements and compliance needs
- 📊 **Enterprise audit** and reporting requirements

💡 **Need help choosing?** See our [Architecture Comparison Guide](docs/ARCHITECTURE-COMPARISON.md) for detailed decision matrices and performance benchmarks.

## 🤝 Contributing

This project is developed and maintained by **Joaquin Lugo Zavala** under the **Diiwo organization**.

### Development Guidelines:
1. Follow established coding patterns from existing implementations
2. Maintain comprehensive test coverage for all new features
3. Update documentation for any API changes
4. Use conventional commit messages
5. Ensure integration with Diiwo.Core remains clean

## 📜 License

MIT License - Copyright © Joaquin Lugo Zavala 2024-2025

See [LICENSE](LICENSE) file for details.

## 📞 Support & Contact

- **Author**: Joaquin Lugo Zavala
- **GitHub**: [@JacobMCfly](https://github.com/JacobMCfly)  
- **Organization**: [Diiwo](https://github.com/diiwo)
- **Repository**: [diiwo/diiwo-identity](https://github.com/diiwo/diiwo-identity)
- **Core Dependency**: [diiwo/diiwo-core](https://github.com/diiwo/diiwo-core)

---

*Built with ❤️ for the .NET community*