# 🔄 Migration Guide - Architecture Transitions

Complete guide for migrating between App Architecture and AspNet Architecture in DIIWO Identity Solution while preserving audit trails and enterprise data integrity.

## 🎯 Table of Contents

- [Migration Overview](#-migration-overview)
- [App to AspNet Migration](#-app-to-aspnet-migration)
- [AspNet to App Migration](#-aspnet-to-app-migration)
- [Data Migration Utilities](#-data-migration-utilities)
- [Validation & Testing](#-validation--testing)
- [Rollback Procedures](#-rollback-procedures)
- [Best Practices](#-best-practices)

## 📊 Migration Overview

### Migration Scenarios

| Scenario | From | To | Complexity | Duration | Risk |
|----------|------|----|-----------|-----------| -----|
| **Startup → Enterprise** | App | AspNet | Medium | 2-4 hours | Low |
| **Enterprise → Microservice** | AspNet | App | High | 4-8 hours | Medium |
| **Architecture Upgrade** | App v1 | App v2 | Low | 1-2 hours | Low |
| **Platform Change** | AspNet | App | High | 6-12 hours | High |

### Key Considerations

- ✅ **Audit Trail Preservation** - All historical audit data must be maintained
- ✅ **Zero Data Loss** - Complete data integrity during migration
- ✅ **Minimal Downtime** - Strategies for near-zero downtime migrations
- ✅ **Rollback Capability** - Safe rollback to previous architecture
- ✅ **Validation** - Comprehensive data validation post-migration

## 🎪➡️🏢 App to AspNet Migration

### When to Migrate App → AspNet

**Scenarios:**
- 🚀 **Startup Growth** - Moving from simple API to full web application
- 🏢 **Enterprise Requirements** - Need for advanced security features
- 🔐 **Compliance Needs** - Regulatory requirements for audit trails
- 👥 **Team Growth** - Multiple developers need standard patterns
- 🌐 **Web Integration** - Adding web UI to existing API

### Pre-Migration Assessment

```csharp
public class AppToAspNetMigrationAssessment
{
    private readonly AppIdentityDbContext _appContext;

    public async Task<MigrationAssessmentReport> AssessAsync()
    {
        var report = new MigrationAssessmentReport
        {
            AssessmentDate = DateTime.UtcNow,
            Architecture = "App → AspNet"
        };

        // Assess data volume
        report.DataVolume = new DataVolumeInfo
        {
            UserCount = await _appContext.Users.CountAsync(),
            PermissionCount = await _appContext.Permissions.CountAsync(),
            SessionCount = await _appContext.UserSessions.CountAsync(),
            LoginHistoryCount = await _appContext.LoginHistory.CountAsync(),
            EstimatedMigrationTime = await EstimateMigrationTimeAsync()
        };

        // Assess custom features
        report.CustomFeatures = await AnalyzeCustomFeaturesAsync();

        // Assess dependencies
        report.Dependencies = await AnalyzeDependenciesAsync();

        // Risk assessment
        report.RiskFactors = AnalyzeRiskFactors();

        return report;
    }

    private async Task<TimeSpan> EstimateMigrationTimeAsync()
    {
        var userCount = await _appContext.Users.CountAsync();
        var permissionCount = await _appContext.Permissions.CountAsync();

        // Estimate: 1000 users = ~10 minutes
        var estimatedMinutes = Math.Max(30, (userCount / 100) + (permissionCount / 500));
        return TimeSpan.FromMinutes(estimatedMinutes);
    }
}
```

### Step 1: Environment Setup

```csharp
// 1. Create AspNet DbContext alongside existing App context
public class MigrationDbContexts
{
    public AppIdentityDbContext AppContext { get; }
    public AspNetIdentityDbContext AspNetContext { get; }

    public MigrationDbContexts(
        AppIdentityDbContext appContext,
        AspNetIdentityDbContext aspNetContext)
    {
        AppContext = appContext;
        AspNetContext = aspNetContext;
    }
}

// 2. Configure dual contexts for migration
public static class MigrationServiceConfiguration
{
    public static IServiceCollection AddMigrationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add both contexts with different connection strings
        services.AddDbContext<AppIdentityDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("AppIdentity")));

        services.AddDbContext<AspNetIdentityDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("AspNetIdentity")));

        // Add migration services
        services.AddScoped<AppToAspNetMigrationService>();
        services.AddScoped<MigrationValidationService>();
        services.AddScoped<RollbackService>();

        return services;
    }
}
```

### Step 2: Data Mapping Strategy

```csharp
public class AppToAspNetDataMapper
{
    // Map App entities to AspNet entities
    public IdentityUser MapToIdentityUser(AppUser appUser)
    {
        return new IdentityUser
        {
            Id = appUser.Id,
            Email = appUser.Email,
            UserName = appUser.Email, // Use email as username
            EmailConfirmed = appUser.EmailConfirmed,
            NormalizedEmail = appUser.Email.ToUpperInvariant(),
            NormalizedUserName = appUser.Email.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),

            // Map custom properties
            FirstName = appUser.FirstName,
            LastName = appUser.LastName,

            // Preserve audit information
            CreatedAt = appUser.CreatedAt,
            UpdatedAt = appUser.UpdatedAt,
            CreatedBy = appUser.CreatedBy,
            UpdatedBy = appUser.UpdatedBy,
            State = appUser.State
        };
    }

    public IdentityPermission MapToIdentityPermission(AppPermission appPermission)
    {
        return new IdentityPermission
        {
            Id = appPermission.Id,
            Name = appPermission.Name,
            Resource = appPermission.Resource,
            Action = appPermission.Action,
            Priority = appPermission.Priority,
            Description = appPermission.Description,

            // Preserve audit trail
            CreatedAt = appPermission.CreatedAt,
            UpdatedAt = appPermission.UpdatedAt,
            CreatedBy = appPermission.CreatedBy,
            UpdatedBy = appPermission.UpdatedBy,
            State = appPermission.State
        };
    }

    public IdentityUserSession MapToIdentityUserSession(AppUserSession appSession)
    {
        return new IdentityUserSession
        {
            Id = appSession.Id,
            UserId = appSession.UserId,
            SessionToken = appSession.SessionToken,
            SessionType = appSession.SessionType,
            ExpiresAt = appSession.ExpiresAt,
            IpAddress = appSession.IpAddress,
            UserAgent = appSession.UserAgent,
            LastActivityAt = appSession.LastActivityAt,

            // Preserve audit trail
            CreatedAt = appSession.CreatedAt,
            UpdatedAt = appSession.UpdatedAt,
            CreatedBy = appSession.CreatedBy,
            UpdatedBy = appSession.UpdatedBy,
            State = appSession.State
        };
    }
}
```

### Step 3: Migration Execution

```csharp
public class AppToAspNetMigrationService
{
    private readonly MigrationDbContexts _contexts;
    private readonly AppToAspNetDataMapper _mapper;
    private readonly ILogger<AppToAspNetMigrationService> _logger;

    public async Task<MigrationResult> ExecuteMigrationAsync(
        MigrationOptions options = null)
    {
        var result = new MigrationResult
        {
            StartTime = DateTime.UtcNow,
            SourceArchitecture = "App",
            TargetArchitecture = "AspNet"
        };

        try
        {
            // 1. Create backup
            if (options?.CreateBackup != false)
            {
                await CreateBackupAsync();
            }

            // 2. Validate source data
            await ValidateSourceDataAsync();

            // 3. Migrate core entities
            await MigrateUsersAsync(result);
            await MigrateRolesAsync(result);
            await MigrateGroupsAsync(result);
            await MigratePermissionsAsync(result);

            // 4. Migrate relationships
            await MigrateUserPermissionsAsync(result);
            await MigrateUserGroupsAsync(result);
            await MigrateGroupPermissionsAsync(result);

            // 5. Migrate audit data
            await MigrateSessionsAsync(result);
            await MigrateLoginHistoryAsync(result);

            // 6. Update password hashes for ASP.NET Core Identity
            await UpdatePasswordHashesAsync(result);

            // 7. Validate migrated data
            await ValidateMigratedDataAsync(result);

            result.IsSuccessful = true;
            result.EndTime = DateTime.UtcNow;

            _logger.LogInformation("App to AspNet migration completed successfully in {Duration}",
                result.Duration);

            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            result.Exception = ex;
            result.EndTime = DateTime.UtcNow;

            _logger.LogError(ex, "App to AspNet migration failed after {Duration}",
                result.Duration);

            // Attempt rollback
            if (options?.AutoRollbackOnFailure != false)
            {
                await RollbackMigrationAsync(result);
            }

            throw;
        }
    }

    private async Task MigrateUsersAsync(MigrationResult result)
    {
        var appUsers = await _contexts.AppContext.Users
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

        var migratedCount = 0;
        foreach (var appUser in appUsers)
        {
            var identityUser = _mapper.MapToIdentityUser(appUser);

            _contexts.AspNetContext.Users.Add(identityUser);
            migratedCount++;

            // Batch save every 100 records for performance
            if (migratedCount % 100 == 0)
            {
                await _contexts.AspNetContext.SaveChangesAsync();
                _logger.LogInformation("Migrated {Count} users", migratedCount);
            }
        }

        await _contexts.AspNetContext.SaveChangesAsync();
        result.EntitiesMigrated["Users"] = migratedCount;

        _logger.LogInformation("Successfully migrated {Count} users", migratedCount);
    }

    private async Task UpdatePasswordHashesAsync(MigrationResult result)
    {
        // ASP.NET Core Identity uses different password hashing
        var users = await _contexts.AspNetContext.Users.ToListAsync();
        var hasher = new PasswordHasher<IdentityUser>();

        foreach (var user in users)
        {
            // Retrieve original password hash from App context
            var appUser = await _contexts.AppContext.Users
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            if (appUser != null)
            {
                // For migration, we can't convert BCrypt to Identity hashes
                // Users will need to reset passwords or use a compatibility layer
                user.PasswordHash = null; // Force password reset
                user.SecurityStamp = Guid.NewGuid().ToString();

                // Mark for password reset
                user.EmailConfirmed = false; // Force email confirmation
            }
        }

        await _contexts.AspNetContext.SaveChangesAsync();
        result.MigrationNotes.Add("Password hashes reset - users will need to reset passwords");
    }
}
```

### Step 4: Password Migration Strategy

```csharp
public class PasswordMigrationService
{
    // Option 1: Force password reset (Recommended)
    public async Task ForcePasswordResetAsync()
    {
        var users = await _aspNetContext.Users.ToListAsync();

        foreach (var user in users)
        {
            user.PasswordHash = null;
            user.EmailConfirmed = false;
            user.SecurityStamp = Guid.NewGuid().ToString();
        }

        await _aspNetContext.SaveChangesAsync();
    }

    // Option 2: Compatibility layer (Advanced)
    public class BCryptCompatibilityPasswordHasher : IPasswordHasher<IdentityUser>
    {
        private readonly PasswordHasher<IdentityUser> _defaultHasher;

        public BCryptCompatibilityPasswordHasher()
        {
            _defaultHasher = new PasswordHasher<IdentityUser>();
        }

        public string HashPassword(IdentityUser user, string password)
        {
            // Use default ASP.NET Core Identity hasher for new passwords
            return _defaultHasher.HashPassword(user, password);
        }

        public PasswordVerificationResult VerifyHashedPassword(
            IdentityUser user, string hashedPassword, string providedPassword)
        {
            // Try BCrypt first (for migrated passwords)
            if (hashedPassword.StartsWith("$2"))
            {
                if (BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword))
                {
                    // Password is correct but using old hash
                    return PasswordVerificationResult.SuccessRehashNeeded;
                }
                return PasswordVerificationResult.Failed;
            }

            // Fall back to default hasher
            return _defaultHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
        }
    }
}
```

## 🏢➡️🎪 AspNet to App Migration

### When to Migrate AspNet → App

**Scenarios:**
- 🚀 **Performance Optimization** - Moving to lighter architecture
- 🎯 **Microservices** - Breaking monolith into services
- 🔧 **Custom Requirements** - Need for non-standard authentication
- 📱 **API-First** - Mobile/API-focused application
- 💰 **Cost Reduction** - Reducing infrastructure complexity

### AspNet to App Migration Service

```csharp
public class AspNetToAppMigrationService
{
    private readonly MigrationDbContexts _contexts;
    private readonly AspNetToAppDataMapper _mapper;
    private readonly ILogger<AspNetToAppMigrationService> _logger;

    public async Task<MigrationResult> ExecuteMigrationAsync(
        MigrationOptions options = null)
    {
        var result = new MigrationResult
        {
            StartTime = DateTime.UtcNow,
            SourceArchitecture = "AspNet",
            TargetArchitecture = "App"
        };

        try
        {
            // 1. Extract ASP.NET Core Identity data
            await ExtractIdentityDataAsync(result);

            // 2. Transform to App architecture
            await TransformToAppEntitiesAsync(result);

            // 3. Preserve custom enterprise features
            await PreserveEnterpriseFeaturesAsync(result);

            // 4. Migrate audit trails
            await MigrateAuditTrailsAsync(result);

            // 5. Validate data integrity
            await ValidateDataIntegrityAsync(result);

            result.IsSuccessful = true;
            result.EndTime = DateTime.UtcNow;

            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;

            _logger.LogError(ex, "AspNet to App migration failed");
            throw;
        }
    }

    private async Task ExtractIdentityDataAsync(MigrationResult result)
    {
        // Extract core Identity tables
        var identityUsers = await _contexts.AspNetContext.Users
            .Include(u => u.UserRoles)
            .Include(u => u.UserClaims)
            .Include(u => u.UserLogins)
            .ToListAsync();

        var identityRoles = await _contexts.AspNetContext.Roles
            .Include(r => r.RoleClaims)
            .ToListAsync();

        // Extract custom entities
        var identityGroups = await _contexts.AspNetContext.IdentityGroups
            .Include(g => g.GroupPermissions)
            .ToListAsync();

        var identityPermissions = await _contexts.AspNetContext.IdentityPermissions
            .ToListAsync();

        result.EntitiesExtracted = new Dictionary<string, int>
        {
            ["IdentityUsers"] = identityUsers.Count,
            ["IdentityRoles"] = identityRoles.Count,
            ["IdentityGroups"] = identityGroups.Count,
            ["IdentityPermissions"] = identityPermissions.Count
        };
    }
}

public class AspNetToAppDataMapper
{
    public AppUser MapToAppUser(IdentityUser identityUser)
    {
        return new AppUser
        {
            Id = identityUser.Id,
            Email = identityUser.Email,
            PasswordHash = identityUser.PasswordHash, // May need conversion
            FirstName = identityUser.FirstName,
            LastName = identityUser.LastName,
            EmailConfirmed = identityUser.EmailConfirmed,

            // Handle ASP.NET Core Identity specific fields
            Username = identityUser.UserName,
            TwoFactorEnabled = identityUser.TwoFactorEnabled,
            LockoutEnd = identityUser.LockoutEnd,
            AccessFailedCount = identityUser.AccessFailedCount,

            // Preserve audit trail
            CreatedAt = identityUser.CreatedAt,
            UpdatedAt = identityUser.UpdatedAt,
            CreatedBy = identityUser.CreatedBy,
            UpdatedBy = identityUser.UpdatedBy,
            State = identityUser.State
        };
    }

    public AppPermission MapToAppPermission(IdentityPermission identityPermission)
    {
        return new AppPermission
        {
            Id = identityPermission.Id,
            Name = identityPermission.Name,
            Resource = identityPermission.Resource,
            Action = identityPermission.Action,
            Priority = identityPermission.Priority,
            Description = identityPermission.Description,

            // Preserve audit trail
            CreatedAt = identityPermission.CreatedAt,
            UpdatedAt = identityPermission.UpdatedAt,
            CreatedBy = identityPermission.CreatedBy,
            UpdatedBy = identityPermission.UpdatedBy,
            State = identityPermission.State
        };
    }
}
```

## 🛠️ Data Migration Utilities

### Migration Command Line Tool

```csharp
public class MigrationCliTool
{
    public static async Task<int> Main(string[] args)
    {
        var command = args.FirstOrDefault()?.ToLower();

        switch (command)
        {
            case "migrate":
                return await HandleMigrateCommand(args);

            case "validate":
                return await HandleValidateCommand(args);

            case "rollback":
                return await HandleRollbackCommand(args);

            case "backup":
                return await HandleBackupCommand(args);

            default:
                ShowHelp();
                return 1;
        }
    }

    private static async Task<int> HandleMigrateCommand(string[] args)
    {
        var options = ParseMigrationOptions(args);

        Console.WriteLine($"Starting migration: {options.Source} → {options.Target}");
        Console.WriteLine($"Connection: {options.SourceConnectionString}");
        Console.WriteLine($"Target: {options.TargetConnectionString}");

        if (!options.Force)
        {
            Console.Write("Are you sure? (y/N): ");
            var confirmation = Console.ReadLine();
            if (!string.Equals(confirmation, "y", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Migration cancelled.");
                return 0;
            }
        }

        var migrationService = CreateMigrationService(options);
        var result = await migrationService.ExecuteMigrationAsync(options);

        if (result.IsSuccessful)
        {
            Console.WriteLine($"Migration completed successfully in {result.Duration}");
            Console.WriteLine($"Entities migrated: {string.Join(", ", result.EntitiesMigrated.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
            return 0;
        }
        else
        {
            Console.WriteLine($"Migration failed: {result.ErrorMessage}");
            return 1;
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("DIIWO Identity Migration Tool");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  migrate --source=app --target=aspnet --source-conn=\"...\" --target-conn=\"...\"");
        Console.WriteLine("  validate --source=app --connection=\"...\"");
        Console.WriteLine("  rollback --backup-path=\"...\" --connection=\"...\"");
        Console.WriteLine("  backup --source=app --connection=\"...\" --output=\"...\"");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --force          Skip confirmation prompts");
        Console.WriteLine("  --dry-run        Validate without making changes");
        Console.WriteLine("  --backup         Create backup before migration");
        Console.WriteLine("  --rollback-on-error  Automatically rollback on failure");
    }
}
```

### Batch Migration Scripts

```sql
-- SQL Script for App to AspNet migration
-- 1. Create AspNet Identity tables
CREATE TABLE [AspNetUsers] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [UserName] NVARCHAR(256) NULL,
    [NormalizedUserName] NVARCHAR(256) NULL,
    [Email] NVARCHAR(256) NULL,
    [NormalizedEmail] NVARCHAR(256) NULL,
    [EmailConfirmed] BIT NOT NULL,
    [PasswordHash] NVARCHAR(MAX) NULL,
    [SecurityStamp] NVARCHAR(MAX) NULL,
    [ConcurrencyStamp] NVARCHAR(MAX) NULL,
    [PhoneNumber] NVARCHAR(MAX) NULL,
    [PhoneNumberConfirmed] BIT NOT NULL,
    [TwoFactorEnabled] BIT NOT NULL,
    [LockoutEnd] DATETIMEOFFSET(7) NULL,
    [LockoutEnabled] BIT NOT NULL,
    [AccessFailedCount] INT NOT NULL,
    -- Custom fields
    [FirstName] NVARCHAR(100) NULL,
    [LastName] NVARCHAR(100) NULL,
    -- Audit fields
    [CreatedAt] DATETIME2(7) NOT NULL,
    [UpdatedAt] DATETIME2(7) NOT NULL,
    [CreatedBy] UNIQUEIDENTIFIER NULL,
    [UpdatedBy] UNIQUEIDENTIFIER NULL,
    [State] INT NOT NULL DEFAULT 1
);

-- 2. Migrate users from App to AspNet
INSERT INTO [AspNetUsers] (
    [Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail],
    [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
    [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled],
    [LockoutEnabled], [AccessFailedCount], [FirstName], [LastName],
    [CreatedAt], [UpdatedAt], [CreatedBy], [UpdatedBy], [State]
)
SELECT
    [Id],
    [Email] AS [UserName],
    UPPER([Email]) AS [NormalizedUserName],
    [Email],
    UPPER([Email]) AS [NormalizedEmail],
    [EmailConfirmed],
    NULL AS [PasswordHash], -- Force password reset
    NEWID() AS [SecurityStamp],
    NEWID() AS [ConcurrencyStamp],
    NULL AS [PhoneNumber],
    0 AS [PhoneNumberConfirmed],
    0 AS [TwoFactorEnabled],
    1 AS [LockoutEnabled],
    0 AS [AccessFailedCount],
    [FirstName],
    [LastName],
    [CreatedAt],
    [UpdatedAt],
    [CreatedBy],
    [UpdatedBy],
    [State]
FROM [AppUsers]
WHERE [State] = 1; -- Only active users

-- 3. Create indexes for performance
CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex]
ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

CREATE NONCLUSTERED INDEX [EmailIndex]
ON [AspNetUsers] ([NormalizedEmail]);
```

## ✅ Validation & Testing

### Comprehensive Validation Service

```csharp
public class MigrationValidationService
{
    public async Task<ValidationReport> ValidateMigrationAsync(
        string sourceArchitecture, string targetArchitecture)
    {
        var report = new ValidationReport
        {
            ValidationDate = DateTime.UtcNow,
            SourceArchitecture = sourceArchitecture,
            TargetArchitecture = targetArchitecture
        };

        // 1. Data integrity validation
        await ValidateDataIntegrityAsync(report);

        // 2. Audit trail validation
        await ValidateAuditTrailsAsync(report);

        // 3. Permission system validation
        await ValidatePermissionSystemAsync(report);

        // 4. Session validation
        await ValidateSessionsAsync(report);

        // 5. Performance validation
        await ValidatePerformanceAsync(report);

        return report;
    }

    private async Task ValidateDataIntegrityAsync(ValidationReport report)
    {
        var checks = new List<ValidationCheck>();

        // User count validation
        var sourceUserCount = await _sourceContext.Users.CountAsync();
        var targetUserCount = await _targetContext.Users.CountAsync();

        checks.Add(new ValidationCheck
        {
            Name = "User Count Validation",
            IsValid = sourceUserCount == targetUserCount,
            Message = $"Source: {sourceUserCount}, Target: {targetUserCount}",
            CheckType = ValidationCheckType.Critical
        });

        // Email uniqueness validation
        var duplicateEmails = await _targetContext.Users
            .GroupBy(u => u.Email)
            .Where(g => g.Count() > 1)
            .CountAsync();

        checks.Add(new ValidationCheck
        {
            Name = "Email Uniqueness",
            IsValid = duplicateEmails == 0,
            Message = duplicateEmails > 0 ? $"Found {duplicateEmails} duplicate emails" : "All emails unique",
            CheckType = ValidationCheckType.Critical
        });

        // Audit trail preservation
        var sourceAuditRecords = await _sourceContext.Users
            .Where(u => u.CreatedAt != default)
            .CountAsync();

        var targetAuditRecords = await _targetContext.Users
            .Where(u => u.CreatedAt != default)
            .CountAsync();

        checks.Add(new ValidationCheck
        {
            Name = "Audit Trail Preservation",
            IsValid = sourceAuditRecords == targetAuditRecords,
            Message = $"Source audit records: {sourceAuditRecords}, Target: {targetAuditRecords}",
            CheckType = ValidationCheckType.Critical
        });

        report.DataIntegrityChecks = checks;
    }

    private async Task ValidatePermissionSystemAsync(ValidationReport report)
    {
        var checks = new List<ValidationCheck>();

        // Permission count validation
        var sourcePermissions = await _sourceContext.Permissions.CountAsync();
        var targetPermissions = await _targetContext.Permissions.CountAsync();

        checks.Add(new ValidationCheck
        {
            Name = "Permission Count",
            IsValid = sourcePermissions == targetPermissions,
            Message = $"Source: {sourcePermissions}, Target: {targetPermissions}",
            CheckType = ValidationCheckType.Critical
        });

        // Permission hierarchy validation
        var sourceHighPriority = await _sourceContext.Permissions
            .Where(p => p.Priority <= 10)
            .CountAsync();

        var targetHighPriority = await _targetContext.Permissions
            .Where(p => p.Priority <= 10)
            .CountAsync();

        checks.Add(new ValidationCheck
        {
            Name = "High Priority Permissions",
            IsValid = sourceHighPriority == targetHighPriority,
            Message = $"Source: {sourceHighPriority}, Target: {targetHighPriority}",
            CheckType = ValidationCheckType.Warning
        });

        report.PermissionSystemChecks = checks;
    }
}
```

### Automated Testing Suite

```csharp
public class MigrationTestSuite
{
    [Test]
    public async Task Migration_PreservesAllUserData()
    {
        // Arrange
        var sourceUsers = await SetupTestUsersAsync(100);

        // Act
        var result = await _migrationService.ExecuteMigrationAsync();

        // Assert
        Assert.IsTrue(result.IsSuccessful);

        var targetUsers = await _targetContext.Users.ToListAsync();
        Assert.AreEqual(sourceUsers.Count, targetUsers.Count);

        foreach (var sourceUser in sourceUsers)
        {
            var targetUser = targetUsers.FirstOrDefault(u => u.Id == sourceUser.Id);
            Assert.IsNotNull(targetUser);
            Assert.AreEqual(sourceUser.Email, targetUser.Email);
            Assert.AreEqual(sourceUser.CreatedAt, targetUser.CreatedAt);
            Assert.AreEqual(sourceUser.CreatedBy, targetUser.CreatedBy);
        }
    }

    [Test]
    public async Task Migration_PreservesAuditTrails()
    {
        // Arrange
        var sourceAuditEntries = await _sourceContext.Users
            .Where(u => u.UpdatedBy != null)
            .CountAsync();

        // Act
        await _migrationService.ExecuteMigrationAsync();

        // Assert
        var targetAuditEntries = await _targetContext.Users
            .Where(u => u.UpdatedBy != null)
            .CountAsync();

        Assert.AreEqual(sourceAuditEntries, targetAuditEntries);
    }

    [Test]
    public async Task Migration_HandlesLargeDatasets()
    {
        // Arrange
        await SetupTestUsersAsync(10000); // 10k users

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _migrationService.ExecuteMigrationAsync();
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.Less(stopwatch.Elapsed, TimeSpan.FromMinutes(30)); // Should complete in 30 minutes
    }
}
```

## 🔄 Rollback Procedures

### Automated Rollback Service

```csharp
public class RollbackService
{
    private readonly ILogger<RollbackService> _logger;

    public async Task<RollbackResult> RollbackMigrationAsync(
        string backupPath, string targetConnectionString)
    {
        var result = new RollbackResult
        {
            StartTime = DateTime.UtcNow,
            BackupPath = backupPath
        };

        try
        {
            // 1. Validate backup integrity
            await ValidateBackupIntegrityAsync(backupPath);

            // 2. Create safety backup of current state
            var safetyBackupPath = await CreateSafetyBackupAsync(targetConnectionString);
            result.SafetyBackupPath = safetyBackupPath;

            // 3. Stop application services
            await StopApplicationServicesAsync();

            // 4. Restore from backup
            await RestoreFromBackupAsync(backupPath, targetConnectionString);

            // 5. Validate restored data
            await ValidateRestoredDataAsync(targetConnectionString);

            // 6. Restart application services
            await StartApplicationServicesAsync();

            result.IsSuccessful = true;
            result.EndTime = DateTime.UtcNow;

            _logger.LogInformation("Rollback completed successfully in {Duration}", result.Duration);

            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;

            _logger.LogError(ex, "Rollback failed after {Duration}", result.Duration);
            throw;
        }
    }

    private async Task RestoreFromBackupAsync(string backupPath, string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // Drop existing database
        var dropCommand = new SqlCommand($@"
            ALTER DATABASE [{GetDatabaseName(connectionString)}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE [{GetDatabaseName(connectionString)}];
        ", connection);

        await dropCommand.ExecuteNonQueryAsync();

        // Restore from backup
        var restoreCommand = new SqlCommand($@"
            RESTORE DATABASE [{GetDatabaseName(connectionString)}]
            FROM DISK = '{backupPath}'
            WITH REPLACE;
        ", connection);

        await restoreCommand.ExecuteNonQueryAsync();
    }
}
```

### Rollback Strategy Decision Matrix

| Scenario | Rollback Method | Recovery Time | Data Loss Risk |
|----------|----------------|---------------|----------------|
| **Migration Failure** | Automatic from backup | 5-15 minutes | None |
| **Data Corruption** | Point-in-time restore | 15-30 minutes | Minimal |
| **Application Issues** | Blue-green deployment | 1-2 minutes | None |
| **Critical Bug** | Immediate service stop + restore | 30-60 minutes | Up to 1 hour |

## 🏆 Best Practices

### 1. Pre-Migration Checklist

- [ ] **Backup Strategy**
  - [ ] Full database backup created
  - [ ] Backup integrity verified
  - [ ] Recovery procedures tested
  - [ ] Backup retention policy defined

- [ ] **Environment Preparation**
  - [ ] Target database created and configured
  - [ ] Application services scaled down
  - [ ] Monitoring systems configured
  - [ ] Rollback procedures documented

- [ ] **Data Assessment**
  - [ ] Data volume analysis completed
  - [ ] Custom features documented
  - [ ] Dependencies mapped
  - [ ] Risk assessment performed

### 2. During Migration

- [ ] **Monitoring**
  - [ ] Progress monitoring active
  - [ ] Performance metrics tracked
  - [ ] Error logging enabled
  - [ ] Resource utilization monitored

- [ ] **Safety Measures**
  - [ ] Batch processing for large datasets
  - [ ] Checkpoint creation for long operations
  - [ ] Error handling and retry logic
  - [ ] Automatic rollback triggers

### 3. Post-Migration

- [ ] **Validation**
  - [ ] Data integrity verification
  - [ ] Audit trail preservation confirmed
  - [ ] Permission system tested
  - [ ] Performance benchmarks met

- [ ] **Documentation**
  - [ ] Migration report generated
  - [ ] Issues and resolutions documented
  - [ ] Performance metrics recorded
  - [ ] Lessons learned captured

### 4. Migration Timing Strategy

```csharp
public class MigrationTimingStrategy
{
    public static MigrationWindow CalculateOptimalMigrationWindow(
        int userCount, TimeSpan estimatedDuration, string timezone = "UTC")
    {
        // Factors to consider:
        // - User activity patterns
        // - Business hours
        // - Backup windows
        // - Maintenance windows

        var window = new MigrationWindow();

        if (userCount < 1000)
        {
            // Small migration - can run during low-traffic hours
            window.PreferredStart = DateTime.Today.AddHours(2); // 2 AM
            window.MaxDuration = TimeSpan.FromHours(2);
            window.MaintenanceRequired = false;
        }
        else if (userCount < 10000)
        {
            // Medium migration - scheduled maintenance window
            window.PreferredStart = DateTime.Today.AddDays(1).AddHours(1); // Next day 1 AM
            window.MaxDuration = TimeSpan.FromHours(4);
            window.MaintenanceRequired = true;
        }
        else
        {
            // Large migration - extended maintenance
            window.PreferredStart = GetNextWeekend().AddHours(1); // Weekend 1 AM
            window.MaxDuration = TimeSpan.FromHours(8);
            window.MaintenanceRequired = true;
            window.RequiresApproval = true;
        }

        window.EstimatedDuration = estimatedDuration;
        window.BufferTime = TimeSpan.FromMinutes(30);

        return window;
    }
}
```

### 5. Communication Plan

```markdown
## Migration Communication Template

### Pre-Migration (T-7 days)
- [ ] Stakeholder notification sent
- [ ] Migration schedule published
- [ ] Impact assessment shared
- [ ] Rollback procedures communicated

### Pre-Migration (T-24 hours)
- [ ] Final migration confirmation
- [ ] Team availability confirmed
- [ ] Backup procedures executed
- [ ] Monitoring systems activated

### During Migration
- [ ] Regular status updates (every 30 minutes)
- [ ] Progress metrics shared
- [ ] Issues reported immediately
- [ ] Stakeholders kept informed

### Post-Migration
- [ ] Success/failure notification
- [ ] Performance metrics shared
- [ ] Issues and resolutions documented
- [ ] Next steps communicated
```

---

*This migration guide ensures safe, reliable transitions between DIIWO Identity Solution architectures while preserving all enterprise audit trails and data integrity.*