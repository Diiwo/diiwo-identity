using Microsoft.EntityFrameworkCore;
using Diiwo.Identity.App.Entities;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.App.DbContext;

/// <summary>
/// APP ARCHITECTURE - Standalone DbContext for simple identity management
/// Independent implementation that doesn't require ASP.NET Core Identity
/// Optimized for simplicity and direct database access
/// Complete 5-level permission system implementation
/// </summary>
public class AppIdentityDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// User accounts for authentication and authorization
    /// </summary>
    public DbSet<AppUser> Users { get; set; } = null!;
    
    /// <summary>
    /// User roles for role-based access control
    /// </summary>
    public DbSet<AppRole> Roles { get; set; } = null!;
    
    /// <summary>
    /// Active user sessions for session management
    /// </summary>
    public DbSet<AppUserSession> UserSessions { get; set; } = null!;
    
    /// <summary>
    /// Login history for security auditing
    /// </summary>
    public DbSet<AppUserLoginHistory> LoginHistory { get; set; } = null!;

    /// <summary>
    /// User groups for group-based permissions
    /// </summary>
    public DbSet<AppGroup> Groups { get; set; } = null!;

    /// <summary>
    /// Permission definitions for the 5-level permission system
    /// </summary>
    public DbSet<AppPermission> Permissions { get; set; } = null!;
    
    /// <summary>
    /// Role-level permissions (Level 1 - highest priority)
    /// </summary>
    public DbSet<AppRolePermission> RolePermissions { get; set; } = null!;
    
    /// <summary>
    /// Group-level permissions (Level 2)
    /// </summary>
    public DbSet<AppGroupPermission> GroupPermissions { get; set; } = null!;
    
    /// <summary>
    /// User-level permissions (Level 3)
    /// </summary>
    public DbSet<AppUserPermission> UserPermissions { get; set; } = null!;
    
    /// <summary>
    /// Model-level permissions (Level 4)
    /// </summary>
    public DbSet<AppModelPermission> ModelPermissions { get; set; } = null!;
    
    /// <summary>
    /// Object-level permissions (Level 5 - lowest priority)
    /// </summary>
    public DbSet<AppObjectPermission> ObjectPermissions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure App entities with "App_" table prefix for clarity
        ConfigureAppUser(modelBuilder);
        ConfigureAppRole(modelBuilder);
        ConfigureAppUserSession(modelBuilder);
        ConfigureAppUserLoginHistory(modelBuilder);
        ConfigureAppGroup(modelBuilder);
        ConfigureAppPermissions(modelBuilder);

        // Seed default data
        SeedDefaultData(modelBuilder);
    }

    private void ConfigureAppUser(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AppUser>();
        
        entity.ToTable("App_Users");
        
        entity.HasKey(u => u.Id);
        
        entity.HasIndex(u => u.Email).IsUnique();
        entity.HasIndex(u => u.Username).IsUnique().HasFilter("[Username] IS NOT NULL");
        entity.HasIndex(u => u.EmailConfirmationToken).HasFilter("[EmailConfirmationToken] IS NOT NULL");
        entity.HasIndex(u => u.PasswordResetToken).HasFilter("[PasswordResetToken] IS NOT NULL");

        entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
        entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
        entity.Property(u => u.FirstName).HasMaxLength(50);
        entity.Property(u => u.LastName).HasMaxLength(50);
        entity.Property(u => u.PhoneNumber).HasMaxLength(20);
        entity.Property(u => u.Username).HasMaxLength(150);
        entity.Property(u => u.LastLoginIp).HasMaxLength(45); // IPv6 compatible
        entity.Property(u => u.TwoFactorSecret).HasMaxLength(255);

        // Relationships
        entity.HasMany(u => u.UserSessions)
              .WithOne(s => s.User)
              .HasForeignKey(s => s.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(u => u.LoginHistory)
              .WithOne(h => h.User)
              .HasForeignKey(h => h.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(u => u.UserPermissions)
              .WithOne(up => up.User)
              .HasForeignKey(up => up.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        // Many-to-many relationship with Groups
        entity.HasMany(u => u.UserGroups)
              .WithMany(g => g.Users)
              .UsingEntity("App_UserGroupMemberships");
    }

    private void ConfigureAppRole(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AppRole>();
        
        entity.ToTable("App_Roles");
        
        entity.HasKey(r => r.Id);
        
        entity.HasIndex(r => r.Name).IsUnique();

        entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
        entity.Property(r => r.Description).HasMaxLength(255);

        entity.HasMany(r => r.RolePermissions)
              .WithOne(rp => rp.Role)
              .HasForeignKey(rp => rp.RoleId)
              .OnDelete(DeleteBehavior.Cascade);
    }

    private void ConfigureAppUserSession(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AppUserSession>();
        
        entity.ToTable("App_UserSessions");
        
        entity.HasKey(s => s.Id);
        
        entity.HasIndex(s => s.SessionToken).IsUnique();
        entity.HasIndex(s => new { s.UserId, s.IsActive });
        entity.HasIndex(s => s.ExpiresAt);

        entity.Property(s => s.SessionToken).IsRequired().HasMaxLength(255);
        entity.Property(s => s.IpAddress).HasMaxLength(45);
        entity.Property(s => s.UserAgent).HasMaxLength(500);
        
        entity.Property(s => s.SessionType).HasConversion<int>();
    }

    private void ConfigureAppUserLoginHistory(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AppUserLoginHistory>();
        
        entity.ToTable("App_LoginHistory");
        
        entity.HasKey(h => h.Id);
        
        entity.HasIndex(h => h.UserId);
        entity.HasIndex(h => h.LoginAttemptAt);
        entity.HasIndex(h => new { h.UserId, h.IsSuccessful });

        entity.Property(h => h.IpAddress).HasMaxLength(45);
        entity.Property(h => h.UserAgent).HasMaxLength(500);
        entity.Property(h => h.FailureReason).HasMaxLength(255);
        
        entity.Property(h => h.AuthMethod).HasConversion<int>();
    }

    private void ConfigureAppGroup(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AppGroup>();
        
        entity.ToTable("App_Groups");
        
        entity.HasKey(g => g.Id);
        
        entity.HasIndex(g => g.Name).IsUnique();

        entity.Property(g => g.Name).IsRequired().HasMaxLength(100);
        entity.Property(g => g.Description).HasMaxLength(255);

        entity.HasMany(g => g.GroupPermissions)
              .WithOne(gp => gp.Group)
              .HasForeignKey(gp => gp.GroupId)
              .OnDelete(DeleteBehavior.Cascade);
    }

    private void ConfigureAppPermissions(ModelBuilder modelBuilder)
    {
        // AppPermission
        var permissionEntity = modelBuilder.Entity<AppPermission>();
        permissionEntity.ToTable("App_Permissions");
        permissionEntity.HasKey(p => p.Id);
        permissionEntity.HasIndex(p => new { p.Resource, p.Action }).IsUnique();
        permissionEntity.Property(p => p.Resource).IsRequired().HasMaxLength(100);
        permissionEntity.Property(p => p.Action).IsRequired().HasMaxLength(100);
        permissionEntity.Property(p => p.Description).HasMaxLength(255);
        permissionEntity.Property(p => p.Scope).HasConversion<int>();

        // AppRolePermission
        var rolePermissionEntity = modelBuilder.Entity<AppRolePermission>();
        rolePermissionEntity.ToTable("App_RolePermissions");
        rolePermissionEntity.HasKey(rp => rp.Id);
        rolePermissionEntity.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
        
        rolePermissionEntity.HasOne(rp => rp.Permission)
                           .WithMany(p => p.RolePermissions)
                           .HasForeignKey(rp => rp.PermissionId)
                           .OnDelete(DeleteBehavior.Cascade);

        // AppGroupPermission
        var groupPermissionEntity = modelBuilder.Entity<AppGroupPermission>();
        groupPermissionEntity.ToTable("App_GroupPermissions");
        groupPermissionEntity.HasKey(gp => gp.Id);
        groupPermissionEntity.HasIndex(gp => new { gp.GroupId, gp.PermissionId }).IsUnique();
        
        groupPermissionEntity.HasOne(gp => gp.Permission)
                            .WithMany(p => p.GroupPermissions)
                            .HasForeignKey(gp => gp.PermissionId)
                            .OnDelete(DeleteBehavior.Cascade);

        // AppUserPermission
        var userPermissionEntity = modelBuilder.Entity<AppUserPermission>();
        userPermissionEntity.ToTable("App_UserPermissions");
        userPermissionEntity.HasKey(up => up.Id);
        userPermissionEntity.HasIndex(up => new { up.UserId, up.PermissionId }).IsUnique();
        userPermissionEntity.HasIndex(up => up.ExpiresAt).HasFilter("[ExpiresAt] IS NOT NULL");
        
        userPermissionEntity.HasOne(up => up.Permission)
                           .WithMany(p => p.UserPermissions)
                           .HasForeignKey(up => up.PermissionId)
                           .OnDelete(DeleteBehavior.Cascade);

        // AppModelPermission
        var modelPermissionEntity = modelBuilder.Entity<AppModelPermission>();
        modelPermissionEntity.ToTable("App_ModelPermissions");
        modelPermissionEntity.HasKey(mp => mp.Id);
        modelPermissionEntity.HasIndex(mp => new { mp.UserId, mp.PermissionId, mp.ModelType }).IsUnique();
        modelPermissionEntity.Property(mp => mp.ModelType).IsRequired().HasMaxLength(100);
        
        modelPermissionEntity.HasOne(mp => mp.Permission)
                            .WithMany(p => p.ModelPermissions)
                            .HasForeignKey(mp => mp.PermissionId)
                            .OnDelete(DeleteBehavior.Cascade);

        modelPermissionEntity.HasOne(mp => mp.User)
                            .WithMany()
                            .HasForeignKey(mp => mp.UserId)
                            .OnDelete(DeleteBehavior.Cascade);

        // AppObjectPermission
        var objectPermissionEntity = modelBuilder.Entity<AppObjectPermission>();
        objectPermissionEntity.ToTable("App_ObjectPermissions");
        objectPermissionEntity.HasKey(op => op.Id);
        objectPermissionEntity.HasIndex(op => new { op.UserId, op.PermissionId, op.ObjectId, op.ObjectType }).IsUnique();
        objectPermissionEntity.Property(op => op.ObjectType).IsRequired().HasMaxLength(100);
        
        objectPermissionEntity.HasOne(op => op.Permission)
                             .WithMany(p => p.ObjectPermissions)
                             .HasForeignKey(op => op.PermissionId)
                             .OnDelete(DeleteBehavior.Cascade);

        objectPermissionEntity.HasOne(op => op.User)
                             .WithMany()
                             .HasForeignKey(op => op.UserId)
                             .OnDelete(DeleteBehavior.Cascade);
    }

    private void SeedDefaultData(ModelBuilder modelBuilder)
    {
        // Seed basic roles
        var roles = new[]
        {
            new AppRole { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "SuperAdmin", Description = "System administrator with full access", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new AppRole { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Admin", Description = "Application administrator", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new AppRole { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "User", Description = "Standard user", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        modelBuilder.Entity<AppRole>().HasData(roles);

        // Seed basic permissions
        var permissions = new[]
        {
            new AppPermission { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Resource = "User", Action = "Read", Description = "View user information", Scope = PermissionScope.Model, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new AppPermission { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Resource = "User", Action = "Write", Description = "Modify user information", Scope = PermissionScope.Model, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new AppPermission { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Resource = "User", Action = "Delete", Description = "Delete users", Scope = PermissionScope.Model, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new AppPermission { Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), Resource = "Admin", Action = "Access", Description = "Access admin panel", Scope = PermissionScope.Global, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new AppPermission { Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), Resource = "Document", Action = "Read", Description = "View documents", Scope = PermissionScope.Object, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new AppPermission { Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), Resource = "Document", Action = "Write", Description = "Edit documents", Scope = PermissionScope.Object, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        modelBuilder.Entity<AppPermission>().HasData(permissions);

        // Seed role permissions (SuperAdmin gets all permissions)
        var rolePermissions = new[]
        {
            new AppRolePermission { Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"), RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"), PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), IsGranted = true, Priority = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new AppRolePermission { Id = Guid.Parse("a2222222-2222-2222-2222-222222222222"), RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"), PermissionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), IsGranted = true, Priority = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new AppRolePermission { Id = Guid.Parse("a3333333-3333-3333-3333-333333333333"), RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"), PermissionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), IsGranted = true, Priority = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new AppRolePermission { Id = Guid.Parse("a4444444-4444-4444-4444-444444444444"), RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"), PermissionId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), IsGranted = true, Priority = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            
            // Admin gets read/write permissions
            new AppRolePermission { Id = Guid.Parse("a5555555-5555-5555-5555-555555555555"), RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222"), PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), IsGranted = true, Priority = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new AppRolePermission { Id = Guid.Parse("a6666666-6666-6666-6666-666666666666"), RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222"), PermissionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), IsGranted = true, Priority = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            
            // User gets read permissions
            new AppRolePermission { Id = Guid.Parse("a7777777-7777-7777-7777-777777777777"), RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333"), PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), IsGranted = true, Priority = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new AppRolePermission { Id = Guid.Parse("a8888888-8888-8888-8888-888888888888"), RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333"), PermissionId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), IsGranted = true, Priority = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        modelBuilder.Entity<AppRolePermission>().HasData(rolePermissions);
    }
}