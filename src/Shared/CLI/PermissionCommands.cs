using System.Reflection;
using Diiwo.Identity.Shared.Tools;
using Diiwo.Identity.Shared.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Diiwo.Identity.Shared.CLI;

/// <summary>
/// Command-line interface for permission management operations.
/// Provides commands for generating permission migrations automatically.
/// </summary>
/// <example>
/// Usage:
/// <code>
/// // In Program.cs or separate CLI tool
/// if (args.Contains("--make-permissions"))
/// {
///     PermissionCommands.MakePermissions();
///     return;
/// }
/// </code>
/// </example>
public static class PermissionCommands
{
    /// <summary>
    /// Generates migration files automatically based on [Permission] attributes.
    /// </summary>
    /// <param name="migrationName">Custom name for the migration</param>
    /// <param name="outputPath">Directory to create migration files (default: ./PermissionMigrations/)</param>
    /// <param name="preview">Preview permissions without generating migration</param>
    public static void MakePermissions(string? migrationName = null, string? outputPath = null, bool preview = false)
    {
        Console.WriteLine("🛠️ Permission Migration Generator");
        Console.WriteLine("============================================");

        try
        {
            // Get assemblies to scan - include entry assembly and its referenced assemblies
            var assemblies = GetAssembliesToScan();
            
            if (preview)
            {
                Console.WriteLine("\n👀 Preview Mode - No files will be created");
                PermissionMigrationGenerator.PreviewPermissions(assemblies);
                return;
            }

            // Generate migration
            var migrationPath = PermissionMigrationGenerator.GenerateMigration(
                migrationName ?? "AutoGeneratePermissions",
                outputPath,
                assemblies
            );

            if (!string.IsNullOrEmpty(migrationPath))
            {
                Console.WriteLine("\n📋 Next Steps:");
                Console.WriteLine("1. Review the generated migration file");
                Console.WriteLine("2. Uncomment the architecture you're using (App or AspNet)");
                Console.WriteLine("3. Run: dotnet ef database update");
                Console.WriteLine("\n✨ Done! Your permissions will be created automatically.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error generating permissions: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner: {ex.InnerException.Message}");
            }
        }
    }

    /// <summary>
    /// Shows help information for permission commands.
    /// </summary>
    public static void ShowHelp()
    {
        Console.WriteLine("🔐 DIIWO Identity - Permission Management Commands");
        Console.WriteLine("==============================================");
        Console.WriteLine();
        Console.WriteLine("🚀 RECOMMENDED - Simple Workflow:");
        Console.WriteLine("  --apply-permissions        Apply permissions directly to database");
        Console.WriteLine();
        Console.WriteLine("📋 Other Commands:");
        Console.WriteLine("  --make-permissions-preview Preview permissions without applying");
        Console.WriteLine("  --make-permissions         Generate migration file (Advanced)");
        Console.WriteLine("  --permissions-help         Show this help message");
        Console.WriteLine();
        Console.WriteLine("Options (for --make-permissions only):");
        Console.WriteLine("  --name <name>              Custom migration name");
        Console.WriteLine("  --output <path>            Output directory (default: ./PermissionMigrations/)");
        Console.WriteLine();
        Console.WriteLine("💡 Examples:");
        Console.WriteLine("  dotnet run -- --apply-permissions         # ✅ RECOMMENDED: Apply to database");
        Console.WriteLine("  dotnet run -- --make-permissions-preview  # 👀 Preview permissions");
        Console.WriteLine("  dotnet run -- --make-permissions          # 🔧 Advanced: Generate migration");
        Console.WriteLine();
        Console.WriteLine("🎯 Simple Workflow (Recommended):");
        Console.WriteLine("  1. Add [Permission] attributes to your entities");
        Console.WriteLine("  2. Run: dotnet run -- --apply-permissions");
        Console.WriteLine("  3. Done! Permissions are now in your database");
        Console.WriteLine();
        Console.WriteLine("⚙️  Advanced Workflow (Migration-based):");
        Console.WriteLine("  1. Add [Permission] attributes to your entities");
        Console.WriteLine("  2. Run: dotnet run -- --make-permissions");
        Console.WriteLine("  3. Review and customize the generated migration");
        Console.WriteLine("  4. Run: dotnet ef database update");
        Console.WriteLine();
    }

    /// <summary>
    /// Processes command-line arguments for permission commands.
    /// </summary>
    /// <param name="args">Command-line arguments</param>
    /// <returns>True if a permission command was processed</returns>
    public static bool ProcessArgs(string[] args)
    {
        if (args.Contains("--permissions-help"))
        {
            ShowHelp();
            return true;
        }

        if (args.Contains("--apply-permissions"))
        {
            ApplyPermissionsDirectly().Wait();
            return true;
        }

        if (args.Contains("--make-permissions-preview"))
        {
            MakePermissions(preview: true);
            return true;
        }

        if (args.Contains("--make-permissions"))
        {
            // Check for custom migration name
            var nameIndex = Array.IndexOf(args, "--name");
            var migrationName = nameIndex >= 0 && nameIndex + 1 < args.Length 
                ? args[nameIndex + 1] 
                : null;

            // Check for custom output path
            var pathIndex = Array.IndexOf(args, "--output");
            var outputPath = pathIndex >= 0 && pathIndex + 1 < args.Length 
                ? args[pathIndex + 1] 
                : null;

            MakePermissions(migrationName, outputPath);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets assemblies to scan for entities with permission attributes.
    /// Includes the entry assembly and commonly referenced assemblies.
    /// </summary>
    /// <returns>Array of assemblies to scan</returns>
    private static Assembly[] GetAssembliesToScan()
    {
        var assemblies = new HashSet<Assembly>();

        // Add entry assembly (main project)
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            assemblies.Add(entryAssembly);
        }

        // Add calling assembly
        var callingAssembly = Assembly.GetCallingAssembly();
        if (callingAssembly != null)
        {
            assemblies.Add(callingAssembly);
        }

        // Add executing assembly (this library)
        assemblies.Add(Assembly.GetExecutingAssembly());

        // Try to add commonly named assemblies that might contain entities
        var commonPrefixes = new[] { "Diiwo", "App", "Domain", "Core", "Entities" };
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in loadedAssemblies)
        {
            var name = assembly.GetName().Name;
            if (name != null && commonPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                assemblies.Add(assembly);
            }
        }

        return assemblies.ToArray();
    }

    /// <summary>
    /// Applies permissions directly to the database without generating migration files.
    /// Auto-detects the architecture (App vs AspNet) and applies permissions accordingly.
    /// </summary>
    public static async Task ApplyPermissionsDirectly()
    {
        Console.WriteLine("🚀 Direct Permission Application");
        Console.WriteLine("================================");
        Console.WriteLine();

        try
        {
            // Get assemblies to scan
            var assemblies = GetAssembliesToScan();

            // Preview what will be applied
            Console.WriteLine("📋 Scanning for entities with [Permission] attributes...");
            PermissionMigrationGenerator.PreviewPermissions(assemblies);

            Console.WriteLine();
            Console.Write("🤔 Apply these permissions to database? (y/N): ");
            var response = Console.ReadLine();

            if (response?.ToLower() != "y" && response?.ToLower() != "yes")
            {
                Console.WriteLine("❌ Operation cancelled.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("🔍 Auto-detecting architecture and applying permissions...");

            // Create a minimal host to use the existing permission generation system
            var host = CreateMinimalHost();

            using (host)
            {
                var permissionsCreated = await host.GeneratePermissionsAsync();

                if (permissionsCreated > 0)
                {
                    Console.WriteLine($"✅ Successfully applied {permissionsCreated} permissions to database!");
                    Console.WriteLine("🎉 All done! Your permissions are now available in the database.");
                }
                else
                {
                    Console.WriteLine("ℹ️  No new permissions were created (they may already exist).");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error applying permissions: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner: {ex.InnerException.Message}");
            }
            Console.WriteLine();
            Console.WriteLine("💡 Troubleshooting tips:");
            Console.WriteLine("   1. Ensure your database connection string is configured");
            Console.WriteLine("   2. Make sure the database exists and is accessible");
            Console.WriteLine("   3. Check that your DbContext is properly registered");
            Console.WriteLine("   4. Verify Entity Framework migrations are up to date");
        }
    }

    /// <summary>
    /// Creates a minimal host with the necessary services for permission generation.
    /// Uses the application's existing configuration and database setup.
    /// </summary>
    /// <returns>Configured host instance</returns>
    private static IHost CreateMinimalHost()
    {
        var builder = new HostBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                // Load configuration from the standard locations
                config.AddJsonFile("appsettings.json", optional: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                      .AddEnvironmentVariables();

                // Add permission generation configuration
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PermissionGeneration:Enabled"] = "true",
                    ["PermissionGeneration:EnableLogging"] = "true"
                });
            })
            .ConfigureServices((context, services) =>
            {
                // Add logging
                services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

                // Try to auto-register database contexts based on what's available
                RegisterAvailableDbContexts(services, context.Configuration);
            })
            .UseEnvironment("Development"); // Force development mode for permission generation

        return builder.Build();
    }

    /// <summary>
    /// Attempts to register available database contexts by scanning loaded assemblies.
    /// This allows the permission system to work without requiring explicit configuration.
    /// </summary>
    /// <param name="services">Service collection to register contexts</param>
    /// <param name="configuration">Application configuration</param>
    private static void RegisterAvailableDbContexts(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = GetConnectionString(configuration);

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "No database connection string found. Please configure a connection string in your appsettings.json:\n" +
                "{\n" +
                "  \"ConnectionStrings\": {\n" +
                "    \"DefaultConnection\": \"your-connection-string-here\"\n" +
                "  }\n" +
                "}");
        }

        // Try to find and register App architecture DbContext
        var appContextType = FindDbContextType("AppIdentityDbContext");
        if (appContextType != null)
        {
            Console.WriteLine("📱 Detected App architecture (AppIdentityDbContext)");
            RegisterDbContext(services, appContextType, connectionString);
        }

        // Try to find and register AspNet architecture DbContext
        var aspNetContextType = FindDbContextType("AspNetIdentityDbContext");
        if (aspNetContextType != null)
        {
            Console.WriteLine("🌐 Detected AspNet architecture (AspNetIdentityDbContext)");
            RegisterDbContext(services, aspNetContextType, connectionString);
        }

        if (appContextType == null && aspNetContextType == null)
        {
            throw new InvalidOperationException(
                "No supported DbContext found. Please ensure either AppIdentityDbContext or AspNetIdentityDbContext is available in your project.");
        }
    }

    /// <summary>
    /// Finds a DbContext type by name across loaded assemblies.
    /// </summary>
    /// <param name="contextTypeName">Name of the DbContext type to find</param>
    /// <returns>Type if found, null otherwise</returns>
    private static Type? FindDbContextType(string contextTypeName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    return Array.Empty<Type>();
                }
            })
            .FirstOrDefault(type => type.Name.Equals(contextTypeName, StringComparison.OrdinalIgnoreCase) &&
                                   type.IsSubclassOf(typeof(DbContext)));
    }

    /// <summary>
    /// Registers a DbContext with the service collection using the provided connection string.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="contextType">Type of DbContext to register</param>
    /// <param name="connectionString">Database connection string</param>
    private static void RegisterDbContext(IServiceCollection services, Type contextType, string connectionString)
    {
        var method = typeof(EntityFrameworkServiceCollectionExtensions)
            .GetMethod("AddDbContext", new[] { typeof(IServiceCollection), typeof(Action<DbContextOptionsBuilder>) })
            ?.MakeGenericMethod(contextType);

        if (method != null)
        {
            method.Invoke(null, new object[]
            {
                services,
                new Action<DbContextOptionsBuilder>(options => options.UseSqlServer(connectionString))
            });
        }
    }

    /// <summary>
    /// Attempts to get a database connection string from configuration.
    /// Tries multiple common configuration keys.
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Connection string if found, null otherwise</returns>
    private static string? GetConnectionString(IConfiguration configuration)
    {
        // Try common connection string names
        var connectionStringKeys = new[]
        {
            "DefaultConnection",
            "Database",
            "Identity",
            "SqlServer",
            "Main"
        };

        foreach (var key in connectionStringKeys)
        {
            var connectionString = configuration.GetConnectionString(key);
            if (!string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine($"📄 Using connection string: {key}");
                return connectionString;
            }
        }

        return null;
    }
}