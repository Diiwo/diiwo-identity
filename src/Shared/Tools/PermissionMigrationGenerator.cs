using System.Reflection;
using System.Text;
using Diiwo.Identity.Shared.Attributes;
using Diiwo.Identity.Shared.Enums;

namespace Diiwo.Identity.Shared.Tools;

/// <summary>
/// Automatic migration generator for permissions.
/// Automatically scans entities with [Permission] attributes and generates migration code.
/// </summary>
/// <remarks>
/// Usage:
/// - PermissionMigrationGenerator.GenerateMigration();
/// - Creates migration file with all permissions automatically
/// - No manual code editing required
/// </remarks>
public static class PermissionMigrationGenerator
{
    /// <summary>
    /// Generates a migration file with all permission definitions automatically.
    /// </summary>
    /// <param name="migrationName">Name for the migration (default: AutoGeneratePermissions)</param>
    /// <param name="outputPath">Directory to create the migration file (default: ./PermissionMigrations/)</param>
    /// <param name="assembliesToScan">Assemblies to scan for entities (default: calling assembly)</param>
    /// <returns>Path to the generated migration file</returns>
    public static string GenerateMigration(
        string migrationName = "AutoGeneratePermissions",
        string? outputPath = null,
        Assembly[]? assembliesToScan = null)
    {
        outputPath ??= Path.Combine(Directory.GetCurrentDirectory(), "PermissionMigrations");
        assembliesToScan ??= new[] { Assembly.GetCallingAssembly() };

        // Scan for entities with permission attributes
        var entityPermissions = ScanForPermissionEntities(assembliesToScan);
        
        if (!entityPermissions.Any())
        {
            Console.WriteLine("❌ No entities with [Permission] attributes found.");
            return string.Empty;
        }

        // Generate migration timestamp
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var migrationClassName = $"{timestamp}_{migrationName}";
        var fileName = $"{migrationClassName}.cs";
        var filePath = Path.Combine(outputPath, fileName);

        // Generate migration code
        var migrationCode = GenerateMigrationCode(migrationClassName, entityPermissions);

        // Ensure output directory exists
        Directory.CreateDirectory(outputPath);

        // Write migration file
        File.WriteAllText(filePath, migrationCode);

        Console.WriteLine($"✅ Generated migration: {fileName}");
        Console.WriteLine($"📁 Location: {filePath}");
        Console.WriteLine($"🔢 Found {entityPermissions.Sum(e => e.Permissions.Length)} permissions across {entityPermissions.Length} entities");

        return filePath;
    }

    /// <summary>
    /// Scans assemblies for entities decorated with PermissionAttribute.
    /// </summary>
    /// <param name="assemblies">Assemblies to scan</param>
    /// <returns>Array of entities with their permission definitions</returns>
    public static EntityPermissionInfo[] ScanForPermissionEntities(Assembly[] assemblies)
    {
        var entityPermissions = new List<EntityPermissionInfo>();

        foreach (var assembly in assemblies)
        {
            try
            {
                var entityTypes = assembly.GetTypes()
                    .Where(type => type.IsClass && 
                                  !type.IsAbstract && 
                                  type.GetCustomAttributes<PermissionAttribute>().Any())
                    .ToArray();

                foreach (var entityType in entityTypes)
                {
                    var permissions = entityType.GetCustomAttributes<PermissionAttribute>().ToArray();
                    
                    entityPermissions.Add(new EntityPermissionInfo
                    {
                        EntityType = entityType,
                        EntityName = entityType.Name,
                        Permissions = permissions
                    });
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that can't be loaded
                continue;
            }
        }

        return entityPermissions.ToArray();
    }

    /// <summary>
    /// Generates the complete migration class code.
    /// </summary>
    /// <param name="className">Migration class name</param>
    /// <param name="entityPermissions">Entity permissions to include</param>
    /// <returns>Complete C# migration file content</returns>
    private static string GenerateMigrationCode(string className, EntityPermissionInfo[] entityPermissions)
    {
        var sb = new StringBuilder();

        // File header
        sb.AppendLine("using Microsoft.EntityFrameworkCore.Migrations;");
        sb.AppendLine("using Diiwo.Identity.Shared.Extensions;");
        sb.AppendLine("using System.Reflection;");
        sb.AppendLine();

        // Namespace and class
        sb.AppendLine("#nullable disable");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated migration for entity permissions.");
        sb.AppendLine($"/// Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"/// Entities: {string.Join(", ", entityPermissions.Select(e => e.EntityName))}");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public partial class {className} : Migration");
        sb.AppendLine("{");

        // Up method
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Applies permission generation for decorated entities.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    protected override void Up(MigrationBuilder migrationBuilder)");
        sb.AppendLine("    {");
        
        // Generate permissions for each architecture
        sb.AppendLine("        // Auto-generate permissions from entity attributes");
        sb.AppendLine("        var currentAssembly = Assembly.GetExecutingAssembly();");
        sb.AppendLine();
        sb.AppendLine("        // App Architecture - Uncomment if using AppIdentityDbContext");
        sb.AppendLine("        // migrationBuilder.GenerateAppEntityPermissions(currentAssembly);");
        sb.AppendLine();
        sb.AppendLine("        // AspNet Architecture - Uncomment if using AspNetIdentityDbContext");  
        sb.AppendLine("        // migrationBuilder.GenerateAspNetEntityPermissions(currentAssembly);");
        sb.AppendLine();

        // Add detailed permission information as comments
        sb.AppendLine("        /*");
        sb.AppendLine("         * Permissions that will be generated:");
        sb.AppendLine("         * ");

        foreach (var entity in entityPermissions)
        {
            sb.AppendLine($"         * {entity.EntityName}:");
            foreach (var permission in entity.Permissions)
            {
                var scopeText = permission.Scope != PermissionScope.Model ? $" (Scope: {permission.Scope})" : "";
                var priorityText = permission.Priority != 0 ? $" (Priority: {permission.Priority})" : "";
                sb.AppendLine($"         *   - {permission.Action}: {permission.Description}{scopeText}{priorityText}");
            }
            sb.AppendLine("         * ");
        }

        sb.AppendLine("         */");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Down method
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Reverts permission generation (removes generated permissions).");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    protected override void Down(MigrationBuilder migrationBuilder)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Remove permissions for the entities in this migration");
        
        var entityNames = string.Join("', '", entityPermissions.Select(e => e.EntityName));
        
        sb.AppendLine("        // App Architecture");
        sb.AppendLine($"        // migrationBuilder.Sql(\"DELETE FROM App_Permissions WHERE Resource IN ('{entityNames}')\");");
        sb.AppendLine();
        sb.AppendLine("        // AspNet Architecture");
        sb.AppendLine($"        // migrationBuilder.Sql(\"DELETE FROM Identity_Permissions WHERE Resource IN ('{entityNames}')\");");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Prints a summary of permissions that would be generated without creating the migration file.
    /// Useful for previewing changes before generation.
    /// </summary>
    /// <param name="assembliesToScan">Assemblies to scan (default: calling assembly)</param>
    public static void PreviewPermissions(Assembly[]? assembliesToScan = null)
    {
        assembliesToScan ??= new[] { Assembly.GetCallingAssembly() };
        var entityPermissions = ScanForPermissionEntities(assembliesToScan);

        if (!entityPermissions.Any())
        {
            Console.WriteLine("❌ No entities with [Permission] attributes found.");
            return;
        }

        Console.WriteLine("🔍 Permission Preview:");
        Console.WriteLine("═══════════════════════");

        foreach (var entity in entityPermissions)
        {
            Console.WriteLine($"\n📋 {entity.EntityName} ({entity.Permissions.Length} permissions):");
            
            foreach (var permission in entity.Permissions)
            {
                var extras = new List<string>();
                if (permission.Scope != PermissionScope.Model) extras.Add($"Scope: {permission.Scope}");
                if (permission.Priority != 0) extras.Add($"Priority: {permission.Priority}");
                
                var extrasText = extras.Any() ? $" ({string.Join(", ", extras)})" : "";
                Console.WriteLine($"  ✅ {permission.Action}: {permission.Description}{extrasText}");
            }
        }

        Console.WriteLine($"\n📊 Summary: {entityPermissions.Sum(e => e.Permissions.Length)} total permissions across {entityPermissions.Length} entities");
    }
}

/// <summary>
/// Information about an entity and its permission attributes.
/// </summary>
public class EntityPermissionInfo
{
    /// <summary>
    /// The entity type that has permission attributes.
    /// </summary>
    public Type EntityType { get; set; } = null!;
    
    /// <summary>
    /// Simple name of the entity (e.g., "Customer", "Product").
    /// </summary>
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// All PermissionAttribute instances found on the entity.
    /// </summary>
    public PermissionAttribute[] Permissions { get; set; } = Array.Empty<PermissionAttribute>();
}