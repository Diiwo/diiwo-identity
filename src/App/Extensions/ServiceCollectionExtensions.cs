using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Diiwo.Identity.App.DbContext;
using Diiwo.Identity.App.Application.Services;

namespace Diiwo.Identity.App.Extensions;

/// <summary>
/// APP ARCHITECTURE - Simple service registration extensions
/// Easy setup for standalone identity management without ASP.NET Core Identity complexity
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds App Identity services with standalone implementation
    /// </summary>
    public static IServiceCollection AddAppIdentity(this IServiceCollection services, IConfiguration configuration, string connectionStringName = "DefaultConnection")
    {
        // Add DbContext
        services.AddDbContext<AppIdentityDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(connectionStringName);
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly("Diiwo.Identity.App");
                npgsql.CommandTimeout(30);
            });

            // Enable sensitive data logging in development
            if (configuration.GetValue<bool>("DetailedErrors", false))
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Add Identity Services
        services.AddScoped<AppUserService>();
        services.AddScoped<AppPermissionService>();

        // Add Authentication (JWT or Cookie-based)
        AddAppAuthentication(services, configuration);

        return services;
    }

    /// <summary>
    /// Adds App Identity services with custom DbContext options
    /// </summary>
    public static IServiceCollection AddAppIdentity(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDbContext)
    {
        // Add DbContext with custom configuration
        services.AddDbContext<AppIdentityDbContext>(configureDbContext);

        // Add Identity Services
        services.AddScoped<AppUserService>();
        services.AddScoped<AppPermissionService>();

        return services;
    }

    /// <summary>
    /// Adds App Identity with in-memory database (for testing)
    /// </summary>
    public static IServiceCollection AddAppIdentityInMemory(this IServiceCollection services, string databaseName = "AppIdentityDb")
    {
        // Add In-Memory DbContext
        services.AddDbContext<AppIdentityDbContext>(options =>
        {
            options.UseInMemoryDatabase(databaseName);
        });

        // Add Identity Services
        services.AddScoped<AppUserService>();
        services.AddScoped<AppPermissionService>();

        return services;
    }

    /// <summary>
    /// Configures authentication for App Identity
    /// </summary>
    private static void AddAppAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        // JWT Authentication (default for API scenarios)
        var jwtSection = configuration.GetSection("Jwt");
        if (jwtSection.Exists())
        {
            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSection["Issuer"],
                        ValidAudience = jwtSection["Audience"],
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                            System.Text.Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!))
                    };
                });
        }
        else
        {
            // Cookie Authentication (fallback for web scenarios)
            services.AddAuthentication("Cookies")
                .AddCookie("Cookies", options =>
                {
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";
                    options.AccessDeniedPath = "/access-denied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                    options.SlidingExpiration = true;
                });
        }

        // Add Authorization
        services.AddAuthorization();
    }

    /// <summary>
    /// Seeds default data (roles, permissions, admin user)
    /// </summary>
    public static async Task<IServiceProvider> SeedAppIdentityDataAsync(this IServiceProvider serviceProvider, string adminEmail = "admin@example.com", string adminPassword = "Admin123!")
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
        var userService = scope.ServiceProvider.GetRequiredService<AppUserService>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Check if admin user exists
        var adminUser = await userService.GetUserByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            // Create admin user
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword);
            adminUser = await userService.CreateUserAsync(adminEmail, hashedPassword, "System", "Administrator");

            // Confirm email for admin
            adminUser.EmailConfirmed = true;
            await userService.UpdateUserAsync(adminUser);

            // TODO: Assign SuperAdmin role to admin user
            // This would require role assignment implementation
        }

        return serviceProvider;
    }

    /// <summary>
    /// Configures CORS for App Identity API
    /// </summary>
    public static IServiceCollection AddAppIdentityCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" };

        services.AddCors(options =>
        {
            options.AddPolicy("AppIdentityPolicy", policy =>
            {
                policy.WithOrigins(corsOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }
}