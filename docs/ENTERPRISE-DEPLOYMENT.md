# 🏢 Enterprise Deployment Guide

Production-ready deployment guide for Diiwo.Identity with automatic audit trails.

## 🎯 Table of Contents

- [Environment Configuration](#️-environment-configuration)
- [Database Setup](#️-database-setup)
- [Security Best Practices](#-security-best-practices)
- [Deployment Checklist](#-deployment-checklist)

## ⚙️ Environment Configuration

### Production appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=YourAppDb;User Id=app_user;Password=${DB_PASSWORD};Encrypt=true;TrustServerCertificate=false;"
  },
  "Identity": {
    "Password": {
      "RequiredLength": 12,
      "RequireUppercase": true,
      "RequireLowercase": true,
      "RequireDigit": true,
      "RequireNonAlphanumeric": true
    },
    "Lockout": {
      "DefaultLockoutTimeSpan": "00:30:00",
      "MaxFailedAccessAttempts": 3,
      "AllowedForNewUsers": true
    },
    "SignIn": {
      "RequireConfirmedEmail": true
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Diiwo.Identity": "Information"
    }
  }
}
```

### Environment Variables

```bash
# Database
DB_PASSWORD=YourSecurePassword123!

# Application
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80

# Security (if using JWT)
JWT_SECRET_KEY=YourVeryLongSecretKeyHere123!

# Audit & Compliance (optional)
AUDIT_RETENTION_DAYS=2555  # 7 years
```

## 🗄️ Database Setup

### SQL Server

```sql
-- 1. Create database
CREATE DATABASE [YourAppDb];

-- 2. Create application user with minimal permissions
CREATE LOGIN [app_user] WITH PASSWORD = 'YourSecurePassword123!';
USE [YourAppDb];
CREATE USER [app_user] FOR LOGIN [app_user];
ALTER ROLE [db_datareader] ADD MEMBER [app_user];
ALTER ROLE [db_datawriter] ADD MEMBER [app_user];
ALTER ROLE [db_ddladmin] ADD MEMBER [app_user]; -- For EF migrations

-- 3. Optional: Create read-only user for reporting
CREATE LOGIN [app_readonly] WITH PASSWORD = 'ReadOnlyPassword123!';
CREATE USER [app_readonly] FOR LOGIN [app_readonly];
ALTER ROLE [db_datareader] ADD MEMBER [app_readonly];
```

### PostgreSQL

```sql
-- 1. Create database
CREATE DATABASE yourappdb
    WITH ENCODING 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8';

-- 2. Create application user
CREATE USER app_user WITH PASSWORD 'YourSecurePassword123!';
GRANT ALL PRIVILEGES ON DATABASE yourappdb TO app_user;

-- 3. Optional: Create read-only user
CREATE USER app_readonly WITH PASSWORD 'ReadOnlyPassword123!';
GRANT CONNECT ON DATABASE yourappdb TO app_readonly;
GRANT USAGE ON SCHEMA public TO app_readonly;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO app_readonly;
```

### Performance Indexes

Add these indexes for better audit trail queries:

```sql
-- For App Architecture
CREATE NONCLUSTERED INDEX IX_AppUsers_UpdatedAt
ON AppUsers (UpdatedAt DESC)
INCLUDE (Id, Email, State);

CREATE NONCLUSTERED INDEX IX_AppUserSessions_UserId_State
ON AppUserSessions (UserId, State)
WHERE State = 1; -- Active sessions only

CREATE NONCLUSTERED INDEX IX_AppUserLoginHistory_UserId_LoginAttemptAt
ON AppUserLoginHistory (UserId, LoginAttemptAt DESC);

-- For AspNet Architecture
CREATE NONCLUSTERED INDEX IX_IdentityUsers_UpdatedAt
ON IdentityUsers (UpdatedAt DESC)
INCLUDE (Id, Email, State);

CREATE NONCLUSTERED INDEX IX_IdentityUserSessions_UserId_State
ON IdentityUserSessions (UserId, State)
WHERE State = 1;

CREATE NONCLUSTERED INDEX IX_IdentityLoginHistory_UserId_LoginAttemptAt
ON IdentityLoginHistory (UserId, LoginAttemptAt DESC);
```

## 🔒 Security Best Practices

### 1. Connection Strings

**✅ DO:**
- Store passwords in environment variables or secrets manager
- Use encrypted connections (`Encrypt=true`)
- Use separate credentials for read/write vs read-only access

**❌ DON'T:**
- Store passwords in appsettings.json
- Use `TrustServerCertificate=true` in production
- Use same credentials for all environments

### 2. Password Policy

**App Architecture:**
```csharp
// Configure BCrypt work factor
services.Configure<BcryptOptions>(options =>
{
    options.WorkFactor = 12; // Higher = more secure but slower
});
```

**AspNet Architecture:**
```csharp
services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 12;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;

    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
});
```

### 3. HTTPS Configuration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Force HTTPS in production
if (builder.Environment.IsProduction())
{
    builder.Services.AddHttpsRedirection(options =>
    {
        options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
        options.HttpsPort = 443;
    });

    builder.Services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
    });
}

var app = builder.Build();

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
```

### 4. Audit Trail Security

The Diiwo.Core audit interceptor automatically tracks:
- ✅ Who created/updated records (`CreatedBy`, `UpdatedBy`)
- ✅ When changes occurred (`CreatedAt`, `UpdatedAt`)
- ✅ Entity state changes (`State`)

Ensure you configure `CurrentUserService`:

```csharp
// Register current user service
services.AddScoped<ICurrentUserService, CurrentUserService>();

// In your authentication middleware
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Guid? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
```

## 📋 Deployment Checklist

### Pre-Deployment

- [ ] **Database**
  - [ ] Production database created
  - [ ] Application user created with minimal permissions
  - [ ] Connection string stored securely (environment variables/secrets)
  - [ ] Performance indexes created

- [ ] **Configuration**
  - [ ] `appsettings.Production.json` configured
  - [ ] Logging configured (console, file, or cloud)
  - [ ] Password policy set appropriately
  - [ ] HTTPS configured with valid certificate

- [ ] **Security**
  - [ ] Secrets stored in secure location (not in appsettings.json)
  - [ ] Email confirmation enabled for production
  - [ ] Account lockout enabled
  - [ ] Audit trail verified working

### Deployment

- [ ] **Application**
  - [ ] Build in Release mode
  - [ ] Run database migrations: `dotnet ef database update`
  - [ ] Verify migrations applied successfully
  - [ ] Test application startup

- [ ] **Verification**
  - [ ] User registration works
  - [ ] User login works
  - [ ] Email confirmation works (if enabled)
  - [ ] Audit trail logging to database
  - [ ] Session management working

### Post-Deployment

- [ ] **Monitoring**
  - [ ] Check logs for errors
  - [ ] Verify database performance
  - [ ] Monitor failed login attempts
  - [ ] Review audit trail entries

- [ ] **Backup**
  - [ ] Configure automated backups
  - [ ] Test backup restoration
  - [ ] Document recovery procedures

## 🚀 Quick Deployment Commands

### Using EF Migrations

```bash
# 1. Install EF tools (if not already installed)
dotnet tool install --global dotnet-ef

# 2. Create initial migration (if needed)
dotnet ef migrations add InitialCreate --project src/Diiwo.Identity.csproj

# 3. Update database to latest migration
dotnet ef database update --project src/Diiwo.Identity.csproj

# 4. Generate SQL script (for review before applying)
dotnet ef migrations script --project src/Diiwo.Identity.csproj --output migration.sql
```

### Using Docker

```bash
# Build image
docker build -t yourapp/identity:latest .

# Run container
docker run -d \
  --name yourapp-identity \
  -p 443:443 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Server=..." \
  -e DB_PASSWORD="..." \
  yourapp/identity:latest
```

## 📊 Monitoring Recommendations

### Health Checks

Add health checks to monitor your application:

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppIdentityDbContext>("database");

app.MapHealthChecks("/health");
```

### Logging

Configure structured logging for better diagnostics:

```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();

    // Optional: Add file logging
    logging.AddFile("logs/app-{Date}.log");

    // Optional: Add cloud logging (Application Insights, etc.)
    logging.AddApplicationInsights();
});
```

## 🔄 Backup Strategy

### Automated Backups (SQL Server)

```sql
-- Full backup daily at midnight
BACKUP DATABASE [YourAppDb]
TO DISK = 'C:\Backups\YourAppDb_Full.bak'
WITH COMPRESSION, INIT;

-- Transaction log backup every hour
BACKUP LOG [YourAppDb]
TO DISK = 'C:\Backups\YourAppDb_Log.trn'
WITH COMPRESSION;
```

### Automated Backups (PostgreSQL)

```bash
# Add to crontab for daily backups
0 0 * * * pg_dump yourappdb | gzip > /backups/yourappdb_$(date +\%Y\%m\%d).sql.gz

# Retention: Keep last 30 days
0 1 * * * find /backups -name "yourappdb_*.sql.gz" -mtime +30 -delete
```

## 📚 Additional Resources

- [Diiwo.Core Documentation](https://github.com/Diiwo/diiwo-core)
- [ASP.NET Core Security Best Practices](https://learn.microsoft.com/en-us/aspnet/core/security/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)

---

**For advanced deployment scenarios** (Kubernetes, load balancing, high availability), refer to your infrastructure team or cloud provider documentation.
