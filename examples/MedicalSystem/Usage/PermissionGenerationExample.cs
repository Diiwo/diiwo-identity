using Microsoft.EntityFrameworkCore;
using Diiwo.Identity.App.DbContext;
using Diiwo.Identity.App.Services;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Services;
using System.Reflection;

namespace MedicalSystem.Usage;

/// <summary>
/// Example showing how to use the automatic permission generation system
/// Demonstrates integration with both App and AspNet architectures
/// </summary>
public static class PermissionGenerationExample
{
    /// <summary>
    /// Example for App Architecture - Generate permissions for medical entities
    /// </summary>
    public static async Task GeneratePermissionsForAppArchitectureAsync()
    {
        // Setup DbContext (in real app, this would be from DI)
        var options = new DbContextOptionsBuilder<AppIdentityDbContext>()
            .UseSqlServer("your-connection-string")
            .Options;

        using var context = new AppIdentityDbContext(options);
        var permissionGenerator = new AppPermissionAutoGenerator();

        // Option 1: Generate permissions for all entities in current assembly
        var medicalAssembly = Assembly.GetExecutingAssembly(); // Assembly containing medical entities
        int permissionsCreated = await permissionGenerator.GeneratePermissionsAsync(context, medicalAssembly);
        
        Console.WriteLine($"Created {permissionsCreated} permissions for medical system");

        // Option 2: Generate permissions for specific entity
        var appointmentPermissions = await permissionGenerator.GeneratePermissionsForEntityAsync(context, typeof(Entities.Appointment));
        Console.WriteLine($"Created {appointmentPermissions} permissions for Appointment entity");

        // Option 3: Preview permissions before creating (useful for validation)
        var patientPermissions = permissionGenerator.GetPermissionDefinitions(typeof(Entities.Patient));
        foreach (var permission in patientPermissions)
        {
            Console.WriteLine($"Would create: {permission.Name} - {permission.Description} (Scope: {permission.Scope}, Priority: {permission.Priority})");
        }
    }

    /// <summary>
    /// Example for AspNet Architecture - Generate permissions for medical entities
    /// </summary>
    public static async Task GeneratePermissionsForAspNetArchitectureAsync()
    {
        // Setup DbContext (in real app, this would be from DI)
        var options = new DbContextOptionsBuilder<AspNetIdentityDbContext>()
            .UseSqlServer("your-connection-string")
            .Options;

        using var context = new AspNetIdentityDbContext(options);
        var permissionGenerator = new AspNetPermissionAutoGenerator();

        // Generate permissions for medical entities
        var medicalAssembly = Assembly.GetExecutingAssembly();
        int permissionsCreated = await permissionGenerator.GeneratePermissionsAsync(context, medicalAssembly);
        
        Console.WriteLine($"Created {permissionsCreated} IdentityPermissions for medical system");
    }

    /// <summary>
    /// Integration example - Use with application startup
    /// </summary>
    public static async Task IntegrateWithStartupAsync(IServiceProvider serviceProvider)
    {
        // Get DbContext from DI container
        using var scope = serviceProvider.CreateScope();
        
        // For App Architecture
        if (scope.ServiceProvider.GetService<AppIdentityDbContext>() is AppIdentityDbContext appContext)
        {
            var appGenerator = new AppPermissionAutoGenerator();
            await appGenerator.GeneratePermissionsAsync(appContext);
        }

        // For AspNet Architecture  
        if (scope.ServiceProvider.GetService<AspNetIdentityDbContext>() is AspNetIdentityDbContext aspNetContext)
        {
            var aspNetGenerator = new AspNetPermissionAutoGenerator();
            await aspNetGenerator.GeneratePermissionsAsync(aspNetContext);
        }
    }

    /// <summary>
    /// Example of generated permissions from the medical entities:
    /// 
    /// From Appointment entity:
    /// - Appointment.View (Model scope, priority 0)
    /// - Appointment.Create (Model scope, priority 0) 
    /// - Appointment.Update (Model scope, priority 0)
    /// - Appointment.Cancel (Model scope, priority 0)
    /// - Appointment.Reschedule (Model scope, priority 0)
    /// - Appointment.ViewHistory (Object scope, priority 0)
    /// - Appointment.ManageWaitlist (Global scope, priority 0)
    /// 
    /// From Patient entity:
    /// - Patient.View (Model scope, priority 10)
    /// - Patient.Create (Global scope, priority 20)
    /// - Patient.Edit (Object scope, priority 15)
    /// - Patient.Archive (Global scope, priority 30)
    /// - Patient.ViewMedicalHistory (Object scope, priority 50)
    /// - Patient.ViewSensitiveInfo (Object scope, priority 100)
    /// - Patient.ExportData (Global scope, priority 40)
    /// </summary>
}