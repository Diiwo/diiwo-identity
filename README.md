# 🎯 DIIWO Identity Solution

**Dual-architecture identity management library for modern .NET applications**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
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

- **🚀 Advanced Permission System**
  - 5-level permission hierarchy with priority-based evaluation
  - Automatic permission generation from entity attributes
  - Simplified workflow for direct database application
  - Traditional migration workflow for enterprise deployment
  - CLI commands for streamlined development

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
  - **Integration with DIIWO-Core** for base entities and shared functionality

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

## 🔧 Dependencies

This project depends on:
- **[DIIWO-Core](https://github.com/diiwo/diiwo-core)** - Base entities and shared functionality
- **.NET 8.0** - Latest .NET framework
- **Entity Framework Core** - Data access and ORM

## 🚀 Quick Start

### Installation

```bash
# Clone the repository
git clone https://github.com/diiwo/diiwo-identity.git

# Navigate to project directory
cd diiwo-identity

# Restore dependencies (including DIIWO-Core)
dotnet restore

# Run tests to verify installation
dotnet test
```

### Current App Architecture Usage

```csharp
// Example of current entity usage
var user = new AppUser
{
    Email = "user@example.com",
    PasswordHash = "hashed-password",
    FirstName = "John",
    LastName = "Doe"
};

// Permission checking (service layer)
var hasPermission = await _permissionService.UserHasPermissionAsync(userId, "Documents", "Read");

// User management (service layer)
var newUser = await _userService.CreateUserAsync("user@example.com", hashedPassword, "John", "Doe");
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

📖 **Documentation:**
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

### When to choose AspNet Architecture:
- 🏢 **Enterprise web applications**
- 🔐 **Complex authentication** scenarios (2FA, OAuth, etc.)
- 🌐 **Web applications** using ASP.NET Core Identity features
- 🎭 **Role-based and policy-based** authorization
- ⚙️ **Integration** with existing ASP.NET Core Identity systems

## 🤝 Contributing

This project is developed and maintained by **Joaquin Lugo Zavala** under the **Diiwo organization**.

### Development Guidelines:
1. Follow established coding patterns from existing implementations
2. Maintain comprehensive test coverage for all new features
3. Update documentation for any API changes
4. Use conventional commit messages
5. Ensure integration with DIIWO-Core remains clean

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