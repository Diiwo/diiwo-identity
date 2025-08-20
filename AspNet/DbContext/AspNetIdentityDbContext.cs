using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.AspNet.DbContext;

/// <summary>
///  ASPNET ARCHITECTURE - Enterprise DbContext extending ASP.NET Core Identity
/// Provides full enterprise features with IdentityDbContext integration
/// Complete 5-level permission system with ASP.NET Core Identity
/// Optional foreign key relationships to App architecture
/// </summary>
public class AspNetIdentityDbContext : IdentityDbContext<IdentityUser, IdentityRole, Guid>
{
    public AspNetIdentityDbContext(DbContextOptions<AspNetIdentityDbContext> options) : base(options)
    {
    }

    // Core entities (extend ASP.NET Identity)
    public DbSet<IdentityUserSession> IdentityUserSessions { get; set; } = null!;
    public DbSet<IdentityLoginHistory> IdentityLoginHistory { get; set; } = null!;

    // Group entities
    public DbSet<IdentityGroup> IdentityGroups { get; set; } = null!;

    // Permission system entities (5-level)
    public DbSet<IdentityPermission> IdentityPermissions { get; set; } = null!;
    public DbSet<IdentityRolePermission> IdentityRolePermissions { get; set; } = null!;
    public DbSet<IdentityGroupPermission> IdentityGroupPermissions { get; set; } = null!;
    public DbSet<IdentityUserPermission> IdentityUserPermissions { get; set; } = null!;
    public DbSet<IdentityModelPermission> IdentityModelPermissions { get; set; } = null!;
    public DbSet<IdentityObjectPermission> IdentityObjectPermissions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ASP.NET Identity tables with "Identity_" prefix
        ConfigureAspNetIdentityTables(modelBuilder);
        
        // Configure custom entities
        ConfigureIdentityUserExtensions(modelBuilder);
        ConfigureIdentityRoleExtensions(modelBuilder);
        ConfigureIdentityUserSession(modelBuilder);
        ConfigureIdentityLoginHistory(modelBuilder);
        ConfigureIdentityGroup(modelBuilder);
        ConfigureIdentityPermissions(modelBuilder);

        // Seed default data
        SeedDefaultData(modelBuilder);
    }

    private void ConfigureAspNetIdentityTables(ModelBuilder modelBuilder)
    {
        // Override ASP.NET Identity table names with "Identity_" prefix
        modelBuilder.Entity<IdentityUser>().ToTable("Identity_Users");
        modelBuilder.Entity<IdentityRole>().ToTable("Identity_Roles");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("Identity_UserRoles");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("Identity_UserClaims");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("Identity_UserLogins");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("Identity_UserTokens");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("Identity_RoleClaims");
    }

    private void ConfigureIdentityUserExtensions(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IdentityUser>();
        
        // Configure additional properties
        entity.Property(u => u.FirstName).HasMaxLength(50);
        entity.Property(u => u.LastName).HasMaxLength(50);
        entity.Property(u => u.LastLoginIp).HasMaxLength(45); // IPv6 compatible
        entity.Property(u => u.TwoFactorSecret).HasMaxLength(255);

        // Indexes for additional properties
        entity.HasIndex(u => u.AppUserId).HasFilter("[AppUserId] IS NOT NULL");

        // Relationships
        entity.HasMany(u => u.IdentityUserSessions)
              .WithOne(s => s.User)
              .HasForeignKey(s => s.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(u => u.IdentityLoginHistory)
              .WithOne(h => h.User)
              .HasForeignKey(h => h.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(u => u.IdentityUserPermissions)
              .WithOne(up => up.User)
              .HasForeignKey(up => up.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        // Many-to-many relationship with Groups
        entity.HasMany(u => u.IdentityUserGroups)
              .WithMany(g => g.Users)
              .UsingEntity("Identity_UserGroupMemberships");

        // Optional relationship to App architecture
        // entity.HasOne(u => u.AppUser)
        //       .WithOne()
        //       .HasForeignKey<IdentityUser>(u => u.AppUserId)
        //       .OnDelete(DeleteBehavior.SetNull);
    }

    private void ConfigureIdentityRoleExtensions(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IdentityRole>();
        
        // Configure additional properties
        entity.Property(r => r.Description).HasMaxLength(255);

        // Relationships
        entity.HasMany(r => r.IdentityRolePermissions)
              .WithOne(rp => rp.Role)
              .HasForeignKey(rp => rp.RoleId)
              .OnDelete(DeleteBehavior.Cascade);

        // Optional relationship to App architecture
        // entity.HasOne(r => r.AppRole)
        //       .WithOne()
        //       .HasForeignKey<IdentityRole>(r => r.AppRoleId)
        //       .OnDelete(DeleteBehavior.SetNull);
    }

    private void ConfigureIdentityUserSession(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IdentityUserSession>();
        
        entity.ToTable("Identity_UserSessions");
        
        entity.HasKey(s => s.Id);
        
        entity.HasIndex(s => s.SessionToken).IsUnique();
        entity.HasIndex(s => new { s.UserId, s.IsActive });
        entity.HasIndex(s => s.ExpiresAt);

        entity.Property(s => s.SessionToken).IsRequired().HasMaxLength(255);
        entity.Property(s => s.IpAddress).HasMaxLength(45);
        entity.Property(s => s.UserAgent).HasMaxLength(500);
        
        entity.Property(s => s.SessionType).HasConversion<int>();
    }

    private void ConfigureIdentityLoginHistory(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IdentityLoginHistory>();
        
        entity.ToTable("Identity_LoginHistory");
        
        entity.HasKey(h => h.Id);
        
        entity.HasIndex(h => h.UserId);
        entity.HasIndex(h => h.LoginAttemptAt);
        entity.HasIndex(h => new { h.UserId, h.IsSuccessful });

        entity.Property(h => h.IpAddress).HasMaxLength(45);
        entity.Property(h => h.UserAgent).HasMaxLength(500);
        entity.Property(h => h.FailureReason).HasMaxLength(255);
        
        entity.Property(h => h.AuthMethod).HasConversion<int>();
    }

    private void ConfigureIdentityGroup(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IdentityGroup>();
        
        entity.ToTable("Identity_Groups");
        
        entity.HasKey(g => g.Id);
        
        entity.HasIndex(g => g.Name).IsUnique();

        entity.Property(g => g.Name).IsRequired().HasMaxLength(100);
        entity.Property(g => g.Description).HasMaxLength(255);

        entity.HasMany(g => g.GroupPermissions)
              .WithOne(gp => gp.Group)
              .HasForeignKey(gp => gp.GroupId)
              .OnDelete(DeleteBehavior.Cascade);

        // Optional relationship to App architecture
        // entity.HasOne(g => g.AppGroup)
        //       .WithOne()
        //       .HasForeignKey<IdentityGroup>(g => g.AppGroupId)
        //       .OnDelete(DeleteBehavior.SetNull);
    }

    private void ConfigureIdentityPermissions(ModelBuilder modelBuilder)
    {
        // IdentityPermission
        var permissionEntity = modelBuilder.Entity<IdentityPermission>();
        permissionEntity.ToTable("Identity_Permissions");
        permissionEntity.HasKey(p => p.Id);
        permissionEntity.HasIndex(p => new { p.Resource, p.Action }).IsUnique();
        permissionEntity.Property(p => p.Resource).IsRequired().HasMaxLength(100);
        permissionEntity.Property(p => p.Action).IsRequired().HasMaxLength(100);
        permissionEntity.Property(p => p.Description).HasMaxLength(255);
        permissionEntity.Property(p => p.Scope).HasConversion<int>();

        // Optional relationship to App architecture
        // permissionEntity.HasOne(p => p.AppPermission)
        //                 .WithOne()
        //                 .HasForeignKey<IdentityPermission>(p => p.AppPermissionId)
        //                 .OnDelete(DeleteBehavior.SetNull);

        // IdentityRolePermission
        var rolePermissionEntity = modelBuilder.Entity<IdentityRolePermission>();
        rolePermissionEntity.ToTable("Identity_RolePermissions");
        rolePermissionEntity.HasKey(rp => rp.Id);
        rolePermissionEntity.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
        
        rolePermissionEntity.HasOne(rp => rp.Permission)
                           .WithMany(p => p.IdentityRolePermissions)
                           .HasForeignKey(rp => rp.PermissionId)
                           .OnDelete(DeleteBehavior.Cascade);

        // IdentityGroupPermission
        var groupPermissionEntity = modelBuilder.Entity<IdentityGroupPermission>();
        groupPermissionEntity.ToTable("Identity_GroupPermissions");
        groupPermissionEntity.HasKey(gp => gp.Id);
        groupPermissionEntity.HasIndex(gp => new { gp.GroupId, gp.PermissionId }).IsUnique();
        
        groupPermissionEntity.HasOne(gp => gp.Permission)
                            .WithMany(p => p.IdentityGroupPermissions)
                            .HasForeignKey(gp => gp.PermissionId)
                            .OnDelete(DeleteBehavior.Cascade);

        // IdentityUserPermission
        var userPermissionEntity = modelBuilder.Entity<IdentityUserPermission>();
        userPermissionEntity.ToTable("Identity_UserPermissions");
        userPermissionEntity.HasKey(up => up.Id);
        userPermissionEntity.HasIndex(up => new { up.UserId, up.PermissionId }).IsUnique();
        userPermissionEntity.HasIndex(up => up.ExpiresAt).HasFilter("[ExpiresAt] IS NOT NULL");
        
        userPermissionEntity.HasOne(up => up.Permission)
                           .WithMany(p => p.IdentityUserPermissions)
                           .HasForeignKey(up => up.PermissionId)
                           .OnDelete(DeleteBehavior.Cascade);

        // IdentityModelPermission
        var modelPermissionEntity = modelBuilder.Entity<IdentityModelPermission>();
        modelPermissionEntity.ToTable("Identity_ModelPermissions");
        modelPermissionEntity.HasKey(mp => mp.Id);
        modelPermissionEntity.HasIndex(mp => new { mp.UserId, mp.PermissionId, mp.ModelType }).IsUnique();
        modelPermissionEntity.Property(mp => mp.ModelType).IsRequired().HasMaxLength(100);
        
        modelPermissionEntity.HasOne(mp => mp.Permission)
                            .WithMany(p => p.IdentityModelPermissions)
                            .HasForeignKey(mp => mp.PermissionId)
                            .OnDelete(DeleteBehavior.Cascade);

        modelPermissionEntity.HasOne(mp => mp.User)
                            .WithMany()
                            .HasForeignKey(mp => mp.UserId)
                            .OnDelete(DeleteBehavior.Cascade);

        // IdentityObjectPermission
        var objectPermissionEntity = modelBuilder.Entity<IdentityObjectPermission>();
        objectPermissionEntity.ToTable("Identity_ObjectPermissions");
        objectPermissionEntity.HasKey(op => op.Id);
        objectPermissionEntity.HasIndex(op => new { op.UserId, op.PermissionId, op.ObjectId, op.ObjectType }).IsUnique();
        objectPermissionEntity.Property(op => op.ObjectType).IsRequired().HasMaxLength(100);
        
        objectPermissionEntity.HasOne(op => op.Permission)
                             .WithMany(p => p.IdentityObjectPermissions)
                             .HasForeignKey(op => op.PermissionId)
                             .OnDelete(DeleteBehavior.Cascade);

        objectPermissionEntity.HasOne(op => op.User)
                             .WithMany()
                             .HasForeignKey(op => op.UserId)
                             .OnDelete(DeleteBehavior.Cascade);
    }

    private void SeedDefaultData(ModelBuilder modelBuilder)
    {
        // Seed basic roles (extending ASP.NET Identity roles)
        var roles = new[]
        {
            new IdentityRole 
            { 
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), 
                Name = "SuperAdmin", 
                NormalizedName = "SUPERADMIN",
                Description = "System administrator with full access", 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new IdentityRole 
            { 
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), 
                Name = "Admin", 
                NormalizedName = "ADMIN",
                Description = "Application administrator", 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new IdentityRole 
            { 
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), 
                Name = "User", 
                NormalizedName = "USER",
                Description = "Standard user", 
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            }
        };

        modelBuilder.Entity<IdentityRole>().HasData(roles);

        // Seed basic permissions
        var permissions = new[]
        {
            new IdentityPermission { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Resource = "User", Action = "Read", Description = "View user information", Scope = PermissionScope.Model, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new IdentityPermission { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Resource = "User", Action = "Write", Description = "Modify user information", Scope = PermissionScope.Model, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new IdentityPermission { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Resource = "User", Action = "Delete", Description = "Delete users", Scope = PermissionScope.Model, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new IdentityPermission { Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), Resource = "Admin", Action = "Access", Description = "Access admin panel", Scope = PermissionScope.Global, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new IdentityPermission { Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), Resource = "Document", Action = "Read", Description = "View documents", Scope = PermissionScope.Object, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new IdentityPermission { Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), Resource = "Document", Action = "Write", Description = "Edit documents", Scope = PermissionScope.Object, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        modelBuilder.Entity<IdentityPermission>().HasData(permissions);

        // Seed role permissions (SuperAdmin gets all permissions)
        var rolePermissions = new[]
        {
            new IdentityRolePermission { Id = Guid.Parse("r1111111-1111-1111-1111-111111111111"), RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"), PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), IsGranted = true, Priority = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new IdentityRolePermission { Id = Guid.Parse("r2222222-2222-2222-2222-222222222222"), RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"), PermissionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), IsGranted = true, Priority = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new IdentityRolePermission { Id = Guid.Parse("r3333333-3333-3333-3333-333333333333"), RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"), PermissionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), IsGranted = true, Priority = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new IdentityRolePermission { Id = Guid.Parse("r4444444-4444-4444-4444-444444444444"), RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"), PermissionId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), IsGranted = true, Priority = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            
            // Admin gets read/write permissions
            new IdentityRolePermission { Id = Guid.Parse("r5555555-5555-5555-5555-555555555555"), RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222"), PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), IsGranted = true, Priority = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new IdentityRolePermission { Id = Guid.Parse("r6666666-6666-6666-6666-666666666666"), RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222"), PermissionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), IsGranted = true, Priority = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            
            // User gets read permissions
            new IdentityRolePermission { Id = Guid.Parse("r7777777-7777-7777-7777-777777777777"), RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333"), PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), IsGranted = true, Priority = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new IdentityRolePermission { Id = Guid.Parse("r8888888-8888-8888-8888-888888888888"), RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333"), PermissionId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), IsGranted = true, Priority = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        modelBuilder.Entity<IdentityRolePermission>().HasData(rolePermissions);
    }
}