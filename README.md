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

### 🏢 **AspNet Architecture** - Enterprise Ready *(Coming Soon)*
Built on ASP.NET Core Identity with enterprise extensions.

- ✅ **Full ASP.NET Core Identity integration**
- ✅ **Compatible** with `UserManager<T>`, `RoleManager<T>`, `SignInManager<T>`
- ✅ **Enterprise features** on top of standard Identity
- ✅ **Ideal for**: Web applications, enterprise systems
- 🗃️ **Entities**: `IdentityUser`, `IdentityRole`, `IdentityGroup`, `IdentityPermission`

## ⚡ Current Implementation Status

### ✅ Recently Completed Features

- **🏗️ App Architecture - Core Implementation**
  - Complete entity model (`AppUser`, `AppRole`, `AppGroup`, `AppPermission`) 
  - Database context with seeding for default roles and permissions
  - Entity relationships and navigation properties
  - Permission system foundation implemented
  
- **🧪 Comprehensive Test Suite**
  - Entity tests with complete documentation and examples
  - Integration tests for database operations and relationships
  - Service tests with mock implementations and business logic validation
  - All tests include detailed assert comments for clarity

- **📦 Project Structure & Dependencies**
  - Solution file with proper project references
  - Updated project metadata and authorship information
  - NuGet package configuration for distribution
  - **Integration with DIIWO-Core** for base entities and shared functionality

### 🚧 Currently In Development

- **🏢 AspNet Architecture** - Next major milestone
  - Entity model extensions of ASP.NET Core Identity
  - Integration with Identity framework services
  - Enterprise features and advanced capabilities

### 📋 Planned Features

- **🌐 Multi-database support** (SQL Server, PostgreSQL, SQLite)
- **📚 Comprehensive documentation** and usage examples
- **🔧 Migration tools** between architectures
- **📦 NuGet package publishing**

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

## 🧪 Testing

The project includes comprehensive test coverage across all implemented features:

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test categories
dotnet test Diiwo-Identity.Tests/App.Tests/ --filter Category=Entity
dotnet test Diiwo-Identity.Tests/App.Tests/ --filter Category=Integration
dotnet test Diiwo-Identity.Tests/App.Tests/ --filter Category=Service
```

**Current Test Structure:**
- **Entity Tests**: Validation of entity logic, relationships, and business rules
- **Integration Tests**: Database context operations and data persistence
- **Service Tests**: Business logic validation with comprehensive mocking

## 📖 Architecture Decision

### When to choose App Architecture:
- 🎯 **Simple applications** with basic user management needs
- 🚀 **Microservices** that need lightweight identity
- 🎮 **Console applications** or background services
- 🔧 **Custom authentication** requirements

### When to choose AspNet Architecture *(Coming Soon)*:
- 🏢 **Enterprise web applications**
- 🔐 **Complex authentication** scenarios (2FA, OAuth, etc.)
- 🌐 **Web applications** using ASP.NET Core Identity features
- 🎭 **Role-based and policy-based** authorization

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