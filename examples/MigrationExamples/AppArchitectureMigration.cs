using Microsoft.EntityFrameworkCore.Migrations;
using Diiwo.Identity.Shared.Extensions;
using System.Reflection;

namespace MigrationExamples;

/// <summary>
/// Example migration for App Architecture showing automatic permission generation
/// Demonstrates how to use MigrationBuilderExtensions for production-safe permission creation
/// </summary>
public partial class AddAppEntityPermissions : Migration
{
    /// <summary>
    /// Generates permissions for all entities in the current assembly
    /// This will scan for any class decorated with PermissionAttribute and create corresponding permissions
    /// </summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Option 1: Generate permissions for current assembly (most common)
        migrationBuilder.GenerateAppEntityPermissions(Assembly.GetExecutingAssembly());

        // Option 2: Generate permissions for specific assembly
        // var medicalAssembly = Assembly.LoadFrom("MedicalSystem.dll");
        // migrationBuilder.GenerateAppEntityPermissions(medicalAssembly);

        // Option 3: Generate permissions for multiple assemblies
        // var assemblies = new[] { 
        //     Assembly.GetExecutingAssembly(), 
        //     typeof(MedicalSystem.Entities.Appointment).Assembly 
        // };
        // migrationBuilder.GenerateEntityPermissions(assemblies, "App_Permissions");

        // Option 4: Generate permissions for specific entity types only
        // var specificTypes = new[] { typeof(Appointment), typeof(Patient) };
        // migrationBuilder.GenerateEntityPermissions(specificTypes, "App_Permissions");
    }

    /// <summary>
    /// Removes generated permissions (optional - for rollback scenarios)
    /// In most cases, you might want to keep permissions even when rolling back
    /// </summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Option 1: Remove all permissions created by this migration
        // This is optional - you might want to keep permissions for data integrity
        
        // migrationBuilder.Sql(@"
        //     DELETE FROM App_Permissions 
        //     WHERE Resource IN ('Appointment', 'Patient', 'Doctor')
        // ");

        // Option 2: Mark permissions as inactive instead of deleting
        // migrationBuilder.Sql(@"
        //     UPDATE App_Permissions 
        //     SET IsActive = 0, UpdatedAt = GETUTCDATE()
        //     WHERE Resource IN ('Appointment', 'Patient', 'Doctor')
        // ");
    }
}

/// <summary>
/// Example: Migration that generates permissions only for medical entities
/// Shows how to be selective about which entities get permission generation
/// </summary>
public partial class AddMedicalPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Get only medical-related entity types
        var medicalTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.Namespace?.Contains("Medical") == true)
            .Where(t => t.GetCustomAttributes<Diiwo.Identity.Shared.Attributes.PermissionAttribute>().Any());

        // Generate permissions only for medical entities
        migrationBuilder.GenerateEntityPermissions(medicalTypes, "App_Permissions");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Clean up medical permissions if needed
        migrationBuilder.Sql(@"
            DELETE FROM App_Permissions 
            WHERE Resource IN ('Appointment', 'Patient', 'Doctor', 'MedicalRecord')
        ");
    }
}

/// <summary>
/// Example: Migration with permission preview for debugging
/// Shows how to validate what permissions would be created before actually creating them
/// Use this approach during development to verify permission generation
/// </summary>
public partial class DebugPermissionGeneration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // During development, you can preview permissions first
        var assembly = Assembly.GetExecutingAssembly();
        var previews = MigrationBuilderExtensions.PreviewEntityPermissions(assembly);

        // Log what would be created (in a real scenario, you'd log this)
        foreach (var preview in previews)
        {
            System.Console.WriteLine($"Would create: {preview.PermissionName} - {preview.Description}");
            System.Console.WriteLine($"  Entity: {preview.EntityType}");
            System.Console.WriteLine($"  Scope: {preview.Scope}");
            System.Console.WriteLine($"  Priority: {preview.Priority}");
            System.Console.WriteLine();
        }

        // After validating the preview, generate the actual permissions
        migrationBuilder.GenerateAppEntityPermissions(assembly);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Rollback logic here
    }
}