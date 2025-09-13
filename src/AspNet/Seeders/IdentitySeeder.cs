using Microsoft.EntityFrameworkCore;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.AspNet.Seeders;

/// <summary>
/// Independent seeder for AspNet Identity architecture
/// Provides controlled seed data insertion separate from migrations
/// Only executes when explicitly called
/// </summary>
public static class IdentitySeeder
{
    /// <summary>
    /// Seeds basic roles, permissions, and role-permission relationships
    /// Only seeds if data doesn't already exist to prevent duplicates
    /// </summary>
    /// <param name="context">AspNet Identity DbContext</param>
    /// <returns>Task for async execution</returns>
    public static async Task SeedAsync(AspNetIdentityDbContext context)
    {
        // Seed roles if they don't exist
        await SeedRolesAsync(context);
        
        // Seed permissions if they don't exist
        await SeedPermissionsAsync(context);
        
        // Seed role-permission relationships
        await SeedRolePermissionsAsync(context);
        
        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(AspNetIdentityDbContext context)
    {
        if (!await context.Roles.AnyAsync())
        {
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

            await context.Roles.AddRangeAsync(roles);
        }
    }

    private static async Task SeedPermissionsAsync(AspNetIdentityDbContext context)
    {
        if (!await context.IdentityPermissions.AnyAsync())
        {
            var permissions = new[]
            {
                new IdentityPermission 
                { 
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 
                    Resource = "User", 
                    Action = "Read", 
                    Description = "View user information", 
                    Scope = PermissionScope.Model, 
                    CreatedAt = DateTime.UtcNow, 
                    UpdatedAt = DateTime.UtcNow 
                },
                new IdentityPermission 
                { 
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 
                    Resource = "User", 
                    Action = "Write", 
                    Description = "Modify user information", 
                    Scope = PermissionScope.Model, 
                    CreatedAt = DateTime.UtcNow, 
                    UpdatedAt = DateTime.UtcNow 
                },
                new IdentityPermission 
                { 
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), 
                    Resource = "User", 
                    Action = "Delete", 
                    Description = "Delete users", 
                    Scope = PermissionScope.Model, 
                    CreatedAt = DateTime.UtcNow, 
                    UpdatedAt = DateTime.UtcNow 
                },
                new IdentityPermission 
                { 
                    Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), 
                    Resource = "Admin", 
                    Action = "Access", 
                    Description = "Access admin panel", 
                    Scope = PermissionScope.Global, 
                    CreatedAt = DateTime.UtcNow, 
                    UpdatedAt = DateTime.UtcNow 
                },
                new IdentityPermission 
                { 
                    Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), 
                    Resource = "Document", 
                    Action = "Read", 
                    Description = "View documents", 
                    Scope = PermissionScope.Object, 
                    CreatedAt = DateTime.UtcNow, 
                    UpdatedAt = DateTime.UtcNow 
                },
                new IdentityPermission 
                { 
                    Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), 
                    Resource = "Document", 
                    Action = "Write", 
                    Description = "Edit documents", 
                    Scope = PermissionScope.Object, 
                    CreatedAt = DateTime.UtcNow, 
                    UpdatedAt = DateTime.UtcNow 
                }
            };

            await context.IdentityPermissions.AddRangeAsync(permissions);
        }
    }

    private static async Task SeedRolePermissionsAsync(AspNetIdentityDbContext context)
    {
        if (!await context.IdentityRolePermissions.AnyAsync())
        {
            var rolePermissions = new[]
            {
                // SuperAdmin gets all permissions
                new IdentityRolePermission 
                { 
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), 
                    RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"), 
                    PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 
                    IsGranted = true, 
                    Priority = 0, 
                    CreatedAt = DateTime.UtcNow, 
                    UpdatedAt = DateTime.UtcNow 
                },
                new IdentityRolePermission 
                { 
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), 
                    RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"), 
                    PermissionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 
                    IsGranted = true, 
                    Priority = 0, 
                    CreatedAt = DateTime.UtcNow, 
                    UpdatedAt = DateTime.UtcNow 
                },
                new IdentityRolePermission 
                { 
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), 
                    RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"), 
                    PermissionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), 
                    IsGranted = true, 
                    Priority = 0, 
                    CreatedAt = DateTime.UtcNow, 
                    UpdatedAt = DateTime.UtcNow 
                },
                new IdentityRolePermission 
                { 
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), 
                    RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"), 
                    PermissionId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), 
                    IsGranted = true, 
                    Priority = 0, 
                    CreatedAt = DateTime.UtcNow, 
                    UpdatedAt = DateTime.UtcNow 
                },
                
                // Admin gets read/write permissions
                new IdentityRolePermission 
                { 
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), 
                    RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222"), 
                    PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 
                    IsGranted = true, 
                    Priority = 0, 
                    CreatedAt = DateTime.UtcNow, 
                    UpdatedAt = DateTime.UtcNow 
                },
                new IdentityRolePermission 
                { 
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), 
                    RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222"), 
                    PermissionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 
                    IsGranted = true, 
                    Priority = 0, 
                    CreatedAt = DateTime.UtcNow, 
                    UpdatedAt = DateTime.UtcNow 
                },
                
                // User gets read permissions only
                new IdentityRolePermission 
                { 
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), 
                    RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333"), 
                    PermissionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 
                    IsGranted = true, 
                    Priority = 0, 
                    CreatedAt = DateTime.UtcNow, 
                    UpdatedAt = DateTime.UtcNow 
                },
                new IdentityRolePermission 
                { 
                    Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), 
                    RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333"), 
                    PermissionId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), 
                    IsGranted = true, 
                    Priority = 0, 
                    CreatedAt = DateTime.UtcNow, 
                    UpdatedAt = DateTime.UtcNow 
                }
            };

            await context.IdentityRolePermissions.AddRangeAsync(rolePermissions);
        }
    }
}