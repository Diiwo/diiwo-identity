# 🛠️ Implementation Examples - Enterprise Patterns

Complete implementation guide with enterprise-ready patterns using DIIWO Identity Solution with automatic audit trails.

## 🎯 Table of Contents

- [Quick Start Examples](#-quick-start-examples)
- [App Architecture Implementation](#-app-architecture-implementation)
- [AspNet Architecture Implementation](#-aspnet-architecture-implementation)
- [Enterprise Patterns](#-enterprise-patterns)
- [Security Best Practices](#-security-best-practices)
- [Performance Optimization](#-performance-optimization)

## 🚀 Quick Start Examples

### App Architecture - 5 Minutes Setup

```csharp
// 1. Install packages
// dotnet add package Microsoft.EntityFrameworkCore.SqlServer
// dotnet add package BCrypt.Net-Next

// 2. Program.cs - Complete setup
using Microsoft.EntityFrameworkCore;
using Diiwo.Core.Extensions;
using Diiwo.Core.Domain.Interfaces;
using Diiwo.Identity.App.DbContext;
using Diiwo.Identity.App.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with automatic audit trails
builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Diiwo.Core for enterprise audit trails
builder.Services.AddDiiwoCore<SystemCurrentUserService>();

// Add Identity services
builder.Services.AddScoped<AppUserService>();
builder.Services.AddScoped<AppPermissionService>();

// Add API controllers
builder.Services.AddControllers();

var app = builder.Build();

// 3. Simple CurrentUserService for APIs
public class SystemCurrentUserService : ICurrentUserService
{
    public Guid? UserId => Guid.Parse("00000000-0000-0000-0000-000000000001");
    public string? UserName => "System";
    public string? UserEmail => "system@company.com";
    public bool IsAuthenticated => true;
    public Task<bool> IsInRoleAsync(string role) => Task.FromResult(true);
}

// 4. Ready to use!
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppUserService _userService;

    public UsersController(AppUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // ✅ Automatic audit trail - no manual code needed!
        var user = await _userService.CreateUserAsync(
            request.Email,
            BCrypt.Net.BCrypt.HashPassword(request.Password),
            request.FirstName,
            request.LastName
        );

        // user.CreatedAt, CreatedBy, etc. automatically populated!
        return Ok(new { user.Id, user.Email, user.CreatedAt });
    }
}
```

### AspNet Architecture - Enterprise Setup

```csharp
// 1. Program.cs - Enterprise configuration
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Diiwo.Core.Extensions;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.AspNet.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with enterprise audit trails
builder.Services.AddDbContext<AspNetIdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add ASP.NET Core Identity with enterprise entities
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Enterprise password policy
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;

    // Enterprise lockout policy
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 3;

    // Enterprise user requirements
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<AspNetIdentityDbContext>()
.AddDefaultTokenProviders();

// Add Diiwo.Core for enterprise audit trails
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, WebCurrentUserService>();
builder.Services.AddDiiwoCoreWithExistingUserService();

// Add enterprise identity services
builder.Services.AddScoped<AspNetUserService>();
builder.Services.AddScoped<AspNetPermissionService>();

// Add MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 2. Enterprise CurrentUserService
public class WebCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WebCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? UserName => _httpContextAccessor.HttpContext?.User?.Identity?.Name;

    public string? UserEmail => _httpContextAccessor.HttpContext?.User?
        .FindFirst(ClaimTypes.Email)?.Value;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Task<bool> IsInRoleAsync(string role)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return Task.FromResult(user?.IsInRole(role) ?? false);
    }
}
```

## 🎪 App Architecture Implementation

### Complete User Management System

```csharp
// 1. Enhanced User Entity with Business Logic
public class AppUser : DomainEntity
{
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Username { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool EmailConfirmed { get; set; } = false;
    public bool PhoneConfirmed { get; set; } = false;
    public bool TwoFactorEnabled { get; set; } = false;
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public string? EmailConfirmationToken { get; set; }
    public DateTime? EmailConfirmationTokenExpiry { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpires { get; set; }
    public string? TwoFactorSecret { get; set; }

    // Navigation properties
    public virtual ICollection<AppUserSession> UserSessions { get; set; } = new List<AppUserSession>();
    public virtual ICollection<AppUserLoginHistory> LoginHistory { get; set; } = new List<AppUserLoginHistory>();
    public virtual ICollection<AppUserPermission> UserPermissions { get; set; } = new List<AppUserPermission>();
    public virtual ICollection<AppGroup> UserGroups { get; set; } = new List<AppGroup>();

    // Business logic methods
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string DisplayName => !string.IsNullOrEmpty(FullName) ? FullName : Email;
    public bool CanLogin => IsActive && EmailConfirmed;

    public void RecordSuccessfulLogin(string? ipAddress = null)
    {
        LastLoginAt = DateTime.UtcNow;
        LastLoginIp = ipAddress;
        FailedLoginAttempts = 0;
        // UpdatedAt and UpdatedBy set automatically!
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 3)
        {
            State = EntityState.Inactive; // Temporary lockout
        }
        // UpdatedAt and UpdatedBy set automatically!
    }
}

// 2. Enterprise User Service with Audit Trails
public class EnterpriseAppUserService : AppUserService
{
    private readonly ILogger<EnterpriseAppUserService> _logger;
    private readonly IEmailService _emailService;

    public EnterpriseAppUserService(
        AppIdentityDbContext context,
        ILogger<EnterpriseAppUserService> logger,
        IEmailService emailService) : base(context, logger)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<AppUser> CreateUserWithNotificationAsync(
        string email, string password, string firstName, string lastName)
    {
        // Create user with automatic audit trail
        var user = await CreateUserAsync(email,
            BCrypt.Net.BCrypt.HashPassword(password), firstName, lastName);

        // Send welcome email
        await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);

        // Log enterprise event
        _logger.LogInformation("Enterprise user created: {UserId} by {CreatedBy} at {CreatedAt}",
            user.Id, user.CreatedBy, user.CreatedAt);

        return user;
    }

    public async Task<bool> AuthenticateAsync(string email, string password)
    {
        var user = await GetUserByEmailAsync(email);
        if (user == null || !user.CanLogin)
        {
            await LogLoginAttemptAsync(user?.Id ?? Guid.Empty, false,
                failureReason: "User not found or locked");
            return false;
        }

        if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            user.RecordSuccessfulLogin();
            await UpdateUserAsync(user);
            await LogLoginAttemptAsync(user.Id, true);

            _logger.LogInformation("Successful authentication for user {UserId}", user.Id);
            return true;
        }
        else
        {
            user.RecordFailedLogin();
            await UpdateUserAsync(user);
            await LogLoginAttemptAsync(user.Id, false, failureReason: "Invalid password");

            _logger.LogWarning("Failed authentication attempt for user {UserId}", user.Id);
            return false;
        }
    }
}

// 3. Enterprise Permission System
public class EnterprisePermissionService : AppPermissionService
{
    public async Task<PermissionAuditReport> GetPermissionAuditTrailAsync(Guid userId)
    {
        var userPermissions = await _context.UserPermissions
            .Include(up => up.Permission)
            .Where(up => up.UserId == userId)
            .Select(up => new PermissionAuditEntry
            {
                PermissionName = up.Permission.Name,
                IsGranted = up.IsGranted,
                GrantedAt = up.CreatedAt,
                GrantedBy = up.CreatedBy,
                LastModified = up.UpdatedAt,
                ModifiedBy = up.UpdatedBy
            })
            .ToListAsync();

        return new PermissionAuditReport
        {
            UserId = userId,
            GeneratedAt = DateTime.UtcNow,
            Permissions = userPermissions
        };
    }

    public async Task<bool> HasPermissionWithAuditAsync(Guid userId, string resource, string action)
    {
        var hasPermission = await HasPermissionAsync(userId, resource, action);

        // Log permission check for compliance
        _logger.LogInformation("Permission check: User {UserId} for {Resource}.{Action} = {Result}",
            userId, resource, action, hasPermission);

        return hasPermission;
    }
}
```

## 🏢 AspNet Architecture Implementation

### Enterprise Web Application

```csharp
// 1. Enhanced Identity User with Enterprise Features
public class EnterpriseIdentityUser : IdentityUser<Guid>, IDomainEntity
{
    public EnterpriseIdentityUser()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        State = EntityState.Active;
    }

    // IDomainEntity implementation - automatic audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public EntityState State { get; set; } = EntityState.Active;

    // Enhanced user properties
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public string? TwoFactorSecret { get; set; }
    public string? ExtendedProperties { get; set; }
    public Guid? AppUserId { get; set; }

    // Navigation properties
    public virtual ICollection<IdentityUserSession> IdentityUserSessions { get; set; } = new List<IdentityUserSession>();
    public virtual ICollection<IdentityLoginHistory> IdentityLoginHistory { get; set; } = new List<IdentityLoginHistory>();
    public virtual ICollection<IdentityUserPermission> IdentityUserPermissions { get; set; } = new List<IdentityUserPermission>();
    public virtual ICollection<IdentityGroup> IdentityUserGroups { get; set; } = new List<IdentityGroup>();

    // Business properties
    public bool IsActive => State == EntityState.Active;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string DisplayName => !string.IsNullOrEmpty(FullName) ? FullName : Email ?? UserName ?? "Unknown";
    public bool CanLogin => !LockoutEnabled && EmailConfirmed && IsActive;

    // Business methods
    public virtual void RecordSuccessfulLogin(string? ipAddress = null)
    {
        LastLoginAt = DateTime.UtcNow;
        LastLoginIp = ipAddress;
        AccessFailedCount = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        State = EntityState.Terminated;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        State = EntityState.Active;
        UpdatedAt = DateTime.UtcNow;
    }
}

// 2. Enterprise Account Controller
[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<EnterpriseIdentityUser> _userManager;
    private readonly SignInManager<EnterpriseIdentityUser> _signInManager;
    private readonly AspNetUserService _userService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<EnterpriseIdentityUser> userManager,
        SignInManager<EnterpriseIdentityUser> signInManager,
        AspNetUserService userService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new EnterpriseIdentityUser
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Department = request.Department,
            JobTitle = request.JobTitle
            // CreatedAt, CreatedBy, etc. set automatically by AuditInterceptor!
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            // Send confirmation email
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await SendConfirmationEmailAsync(user.Email, token);

            // Log successful registration with audit info
            _logger.LogInformation("User registered: {UserId} ({Email}) at {CreatedAt}",
                user.Id, user.Email, user.CreatedAt);

            return Ok(new { message = "Registration successful", userId = user.Id });
        }

        return BadRequest(result.Errors);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _signInManager.PasswordSignInAsync(
            request.Email, request.Password, request.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            // UpdatedAt and UpdatedBy set automatically!

            await _userService.LogLoginAttemptAsync(user.Id, true);

            return Ok(new { message = "Login successful", userId = user.Id });
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account locked out for email: {Email}", request.Email);
            return BadRequest("Account is locked out");
        }

        await _userService.LogLoginAttemptAsync(Guid.Empty, false,
            failureReason: "Invalid credentials");

        return BadRequest("Invalid login attempt");
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Department,
            user.JobTitle,
            user.LastLoginAt,
            user.CreatedAt,
            user.UpdatedAt,
            AuditInfo = new
            {
                CreatedBy = user.CreatedBy,
                UpdatedBy = user.UpdatedBy,
                LastModified = user.UpdatedAt
            }
        });
    }
}

// 3. Enterprise Permission Controller
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly AspNetPermissionService _permissionService;
    private readonly UserManager<EnterpriseIdentityUser> _userManager;

    [HttpPost("grant/{userId}")]
    public async Task<IActionResult> GrantPermission(
        Guid userId, [FromBody] GrantPermissionRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        var success = await _permissionService.AssignPermissionToUserAsync(
            userId, request.PermissionId, request.IsGranted);

        if (success)
        {
            // Permission grant automatically audited by AuditInterceptor
            return Ok(new { message = "Permission granted successfully" });
        }

        return BadRequest("Failed to grant permission");
    }

    [HttpGet("audit/{userId}")]
    public async Task<IActionResult> GetPermissionAudit(Guid userId)
    {
        var permissions = await _permissionService.GetUserPermissionAuditAsync(userId);
        return Ok(permissions);
    }
}
```

## 🏢 Enterprise Patterns

### 1. Compliance and Audit Reporting

```csharp
public class ComplianceReportingService
{
    private readonly AppIdentityDbContext _context;

    public async Task<ComplianceReport> GenerateUserActivityReportAsync(
        DateTime fromDate, DateTime toDate)
    {
        var userActivity = await _context.Users
            .Where(u => u.UpdatedAt >= fromDate && u.UpdatedAt <= toDate)
            .Select(u => new UserActivityEntry
            {
                UserId = u.Id,
                Email = u.Email,
                LastActivity = u.UpdatedAt,
                ModifiedBy = u.UpdatedBy,
                CurrentState = u.State,
                IsActive = u.IsActive
            })
            .ToListAsync();

        var permissionChanges = await _context.UserPermissions
            .Where(up => up.UpdatedAt >= fromDate && up.UpdatedAt <= toDate)
            .Include(up => up.User)
            .Include(up => up.Permission)
            .Select(up => new PermissionChangeEntry
            {
                UserId = up.UserId,
                UserEmail = up.User.Email,
                PermissionName = up.Permission.Name,
                IsGranted = up.IsGranted,
                ChangedAt = up.UpdatedAt,
                ChangedBy = up.UpdatedBy
            })
            .ToListAsync();

        return new ComplianceReport
        {
            ReportPeriod = new DateRange(fromDate, toDate),
            GeneratedAt = DateTime.UtcNow,
            UserActivities = userActivity,
            PermissionChanges = permissionChanges,
            Summary = new ComplianceSummary
            {
                TotalUsers = userActivity.Count,
                ActiveUsers = userActivity.Count(u => u.IsActive),
                PermissionChanges = permissionChanges.Count,
                SecurityEvents = permissionChanges.Count(p => !p.IsGranted)
            }
        };
    }
}
```

### 2. Data Retention and Archival

```csharp
public class DataRetentionService
{
    private readonly AppIdentityDbContext _context;

    public async Task<DataRetentionReport> ArchiveInactiveUsersAsync(
        DateTime cutoffDate, bool dryRun = false)
    {
        var inactiveUsers = await _context.Users
            .Where(u => u.LastLoginAt < cutoffDate && u.State == EntityState.Active)
            .ToListAsync();

        var archivalCandidates = new List<UserArchivalEntry>();

        foreach (var user in inactiveUsers)
        {
            var archivalEntry = new UserArchivalEntry
            {
                UserId = user.Id,
                Email = user.Email,
                LastLogin = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                DataSize = await CalculateUserDataSizeAsync(user.Id)
            };

            archivalCandidates.Add(archivalEntry);

            if (!dryRun)
            {
                // Soft delete - preserves audit trail
                user.State = EntityState.Terminated;
                // UpdatedAt and UpdatedBy set automatically by AuditInterceptor
            }
        }

        if (!dryRun)
        {
            await _context.SaveChangesAsync();
        }

        return new DataRetentionReport
        {
            CutoffDate = cutoffDate,
            ProcessedAt = DateTime.UtcNow,
            IsDryRun = dryRun,
            UsersArchived = archivalCandidates.Count,
            TotalDataArchived = archivalCandidates.Sum(u => u.DataSize),
            ArchivedUsers = archivalCandidates
        };
    }

    private async Task<long> CalculateUserDataSizeAsync(Guid userId)
    {
        // Calculate size of all user-related data
        var userSessions = await _context.UserSessions.CountAsync(s => s.UserId == userId);
        var loginHistory = await _context.LoginHistory.CountAsync(h => h.UserId == userId);
        var permissions = await _context.UserPermissions.CountAsync(p => p.UserId == userId);

        // Estimate data size (simplified calculation)
        return (userSessions * 500) + (loginHistory * 200) + (permissions * 100);
    }
}
```

### 3. Security Monitoring

```csharp
public class SecurityMonitoringService
{
    private readonly AppIdentityDbContext _context;
    private readonly ILogger<SecurityMonitoringService> _logger;

    public async Task<SecurityAlert[]> DetectSuspiciousActivityAsync()
    {
        var alerts = new List<SecurityAlert>();
        var yesterday = DateTime.UtcNow.AddDays(-1);

        // Detect multiple failed login attempts
        var suspiciousLogins = await _context.LoginHistory
            .Where(h => h.LoginAttemptAt >= yesterday && !h.IsSuccessful)
            .GroupBy(h => h.UserId)
            .Where(g => g.Count() >= 5)
            .Select(g => new { UserId = g.Key, FailedAttempts = g.Count() })
            .ToListAsync();

        foreach (var suspicious in suspiciousLogins)
        {
            alerts.Add(new SecurityAlert
            {
                Type = SecurityAlertType.MultipleFailedLogins,
                UserId = suspicious.UserId,
                Severity = SecuritySeverity.Medium,
                Description = $"User has {suspicious.FailedAttempts} failed login attempts in 24 hours",
                DetectedAt = DateTime.UtcNow
            });
        }

        // Detect permission escalations
        var recentPermissionGrants = await _context.UserPermissions
            .Where(up => up.CreatedAt >= yesterday && up.IsGranted)
            .Include(up => up.User)
            .Include(up => up.Permission)
            .ToListAsync();

        var highValuePermissions = recentPermissionGrants
            .Where(up => up.Permission.Resource == "Admin" ||
                        up.Permission.Action == "Delete" ||
                        up.Permission.Priority <= 10)
            .ToList();

        foreach (var permission in highValuePermissions)
        {
            alerts.Add(new SecurityAlert
            {
                Type = SecurityAlertType.PermissionEscalation,
                UserId = permission.UserId,
                Severity = SecuritySeverity.High,
                Description = $"High-value permission granted: {permission.Permission.Name}",
                DetectedAt = DateTime.UtcNow,
                AuditInfo = new AuditInfo
                {
                    GrantedBy = permission.CreatedBy,
                    GrantedAt = permission.CreatedAt
                }
            });
        }

        return alerts.ToArray();
    }
}
```

## 🔒 Security Best Practices

### 1. Password Security

```csharp
public class SecurePasswordService
{
    public string HashPassword(string password)
    {
        // Use BCrypt with high work factor for enterprise security
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool ValidatePasswordComplexity(string password)
    {
        // Enterprise password policy
        return password.Length >= 12 &&
               password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c)) &&
               !ContainsCommonPasswords(password);
    }

    private bool ContainsCommonPasswords(string password)
    {
        var commonPasswords = new[]
        {
            "password", "123456", "admin", "welcome", "company"
        };
        return commonPasswords.Any(common =>
            password.ToLower().Contains(common.ToLower()));
    }
}
```

### 2. Session Security

```csharp
public class SecureSessionService
{
    public async Task<string> CreateSecureSessionAsync(Guid userId)
    {
        // Generate cryptographically secure session token
        var sessionToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var session = new AppUserSession
        {
            UserId = userId,
            SessionToken = sessionToken,
            ExpiresAt = DateTime.UtcNow.AddHours(8), // 8-hour sessions
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent()
            // CreatedAt, CreatedBy set automatically by AuditInterceptor
        };

        await _context.UserSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        return sessionToken;
    }

    public async Task<bool> ValidateSessionAsync(string sessionToken)
    {
        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

        if (session == null || session.ExpiresAt <= DateTime.UtcNow || !session.IsActive)
        {
            return false;
        }

        // Check for session hijacking
        var currentIp = GetClientIpAddress();
        if (session.IpAddress != currentIp)
        {
            // Log security event
            _logger.LogWarning("Session IP mismatch for user {UserId}: expected {ExpectedIp}, got {ActualIp}",
                session.UserId, session.IpAddress, currentIp);

            // Revoke session for security
            session.IsActive = false;
            await _context.SaveChangesAsync();
            return false;
        }

        return true;
    }
}
```

## ⚡ Performance Optimization

### 1. Efficient Queries

```csharp
public class OptimizedUserService
{
    // Use projection to avoid loading unnecessary data
    public async Task<UserSummaryDto[]> GetUserSummariesAsync(int pageSize, int pageNumber)
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Email)
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .Select(u => new UserSummaryDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FirstName + " " + u.LastName,
                LastLogin = u.LastLoginAt,
                CreatedAt = u.CreatedAt
                // Only select needed fields
            })
            .AsNoTracking() // Read-only queries
            .ToArrayAsync();
    }

    // Bulk operations for better performance
    public async Task<int> BulkUpdateUserStatusAsync(Guid[] userIds, EntityState newState)
    {
        return await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.State, newState)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));
                // AuditInterceptor handles UpdatedBy automatically
    }
}
```

### 2. Caching Strategies

```csharp
public class CachedPermissionService
{
    private readonly IMemoryCache _cache;
    private readonly AppPermissionService _permissionService;

    public async Task<bool> HasPermissionAsync(Guid userId, string resource, string action)
    {
        var cacheKey = $"permission:{userId}:{resource}:{action}";

        if (_cache.TryGetValue(cacheKey, out bool cachedResult))
        {
            return cachedResult;
        }

        var result = await _permissionService.HasPermissionAsync(userId, resource, action);

        // Cache for 5 minutes
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

        return result;
    }

    public void InvalidateUserPermissions(Guid userId)
    {
        // Remove all cached permissions for user
        var cacheKeys = GetUserPermissionCacheKeys(userId);
        foreach (var key in cacheKeys)
        {
            _cache.Remove(key);
        }
    }
}
```

## 📚 Related Documentation

- [Architecture Comparison](ARCHITECTURE-COMPARISON.md) - Choose the right architecture
- [Enterprise Deployment](ENTERPRISE-DEPLOYMENT.md) - Production deployment guide
- [Migration Guide](MIGRATION-GUIDE.md) - Architecture transition strategies
- [Performance Guide](PERFORMANCE-GUIDE.md) - Advanced optimization techniques

---

*These examples demonstrate production-ready patterns with automatic audit trails and enterprise security features.*