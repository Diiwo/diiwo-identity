using Diiwo.Identity.Shared.CLI;

namespace Diiwo.Identity.Shared.Extensions;

/// <summary>
/// Extension methods for integrating permission CLI commands into applications.
/// Provides command processing for permission management.
/// </summary>
public static class CliExtensions
{
    /// <summary>
    /// Processes permission-related command-line arguments before starting the application.
    /// Returns true if a CLI command was processed and the app should exit.
    /// </summary>
    /// <param name="args">Command-line arguments from Main method</param>
    /// <returns>True if app should exit (command was processed), False if app should continue normally</returns>
    /// <example>
    /// Basic usage in Program.cs:
    /// <code>
    /// // Process CLI commands first
    /// if (args.ProcessPermissionCommands())
    /// {
    ///     return; // Exit after processing command
    /// }
    /// 
    /// // Continue with normal app startup
    /// var builder = WebApplication.CreateBuilder(args);
    /// // ... rest of your app setup
    /// </code>
    /// 
    /// Advanced usage with error handling:
    /// <code>
    /// try
    /// {
    ///     if (args.ProcessPermissionCommands())
    ///     {
    ///         return 0; // Success exit code
    ///     }
    /// }
    /// catch (Exception ex)
    /// {
    ///     Console.WriteLine($"Command failed: {ex.Message}");
    ///     return 1; // Error exit code
    /// }
    /// </code>
    /// </example>
    public static bool ProcessPermissionCommands(this string[]? args)
    {
        // Handle null arguments gracefully
        if (args == null)
        {
            return false;
        }

        // Show help if help requested and permission commands are present
        if (args.Contains("--help") || args.Contains("-h"))
        {
            var hasPermissionCommands = args.Any(arg => arg.StartsWith("--make-permissions") ||
                                                       arg.StartsWith("--permissions"));
            if (hasPermissionCommands)
            {
                PermissionCommands.ShowHelp();
                return true; // Exit after showing help
            }
        }

        // Process permission commands
        return PermissionCommands.ProcessArgs(args);
    }

}