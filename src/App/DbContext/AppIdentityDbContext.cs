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

}