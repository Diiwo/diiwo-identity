using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Diiwo.Identity.AspNet.DbContext;
using Diiwo.Identity.AspNet.Entities;
using Diiwo.Identity.AspNet.Application.Services;
using Diiwo.Identity.AspNet.Abstractions.Services;
using Diiwo.Identity.Shared.Abstractions.Services;

namespace Diiwo.Identity.AspNet.Extensions;

/// <summary>
///  ASPNET ARCHITECTURE - Enterprise service registration extensions
/// Full ASP.NET Core Identity integration with enterprise features
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds AspNet Identity services with full ASP.NET Core Identity integration
    /// </summary>
    public static IServiceCollection AddAspNetIdentity(this IServiceCollection services, IConfiguration configuration, string connectionStringName = "DefaultConnection")
    {
        // Add DbContext
        services.AddDbContext<AspNetIdentityDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(connectionStringName);
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly("Diiwo.Identity.AspNet");
                npgsql.CommandTimeout(30);
            });

            // Enable sensitive data logging in development
            if (configuration.GetValue<bool>("DetailedErrors", false))
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Add ASP.NET Core Identity
        services.AddIdentity<Entities.IdentityUser, Entities.IdentityRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;

            // Sign in settings
            options.SignIn.RequireConfirmedEmail = true;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<AspNetIdentityDbContext>()
        .AddDefaultTokenProviders();

        // Add custom Identity services
        services.AddScoped<IAspNetUserService, AspNetUserService>();
        services.AddScoped<IAspNetPermissionService, AspNetPermissionService>();
        services.AddScoped<IIdentityPermissionService, IdentityPermissionService>();

        // Configure Authentication
        ConfigureAspNetAuthentication(services, configuration);

        return services;
    }

    /// <summary>
    /// Adds AspNet Identity services with custom DbContext options
    /// </summary>
    public static IServiceCollection AddAspNetIdentity(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDbContext, Action<Microsoft.AspNetCore.Identity.IdentityOptions>? configureIdentity = null)
    {
        // Add DbContext with custom configuration
        services.AddDbContext<AspNetIdentityDbContext>(configureDbContext);

        // Add ASP.NET Core Identity
        var identityBuilder = services.AddIdentity<Entities.IdentityUser, Entities.IdentityRole>(configureIdentity ?? DefaultIdentityOptions)
            .AddEntityFrameworkStores<AspNetIdentityDbContext>()
            .AddDefaultTokenProviders();

        // Add custom Identity services
        services.AddScoped<IAspNetUserService, AspNetUserService>();
        services.AddScoped<IAspNetPermissionService, AspNetPermissionService>();
        services.AddScoped<IIdentityPermissionService, IdentityPermissionService>();

        return services;
    }

    /// <summary>
    /// Adds AspNet Identity with in-memory database (for testing)
    /// </summary>
    public static IServiceCollection AddAspNetIdentityInMemory(this IServiceCollection services, string databaseName = "AspNetIdentityDb")
    {
        // Add In-Memory DbContext
        services.AddDbContext<AspNetIdentityDbContext>(options =>
        {
            options.UseInMemoryDatabase(databaseName);
        });

        // Add ASP.NET Core Identity
        services.AddIdentity<Entities.IdentityUser, Entities.IdentityRole>(DefaultIdentityOptions)
            .AddEntityFrameworkStores<AspNetIdentityDbContext>()
            .AddDefaultTokenProviders();

        // Add custom Identity services
        services.AddScoped<IAspNetUserService, AspNetUserService>();
        services.AddScoped<IAspNetPermissionService, AspNetPermissionService>();
        services.AddScoped<IIdentityPermissionService, IdentityPermissionService>();

        return services;
    }

    /// <summary>
    /// Configures authentication for AspNet Identity
    /// </summary>
    private static void ConfigureAspNetAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        // Cookie Authentication (default for ASP.NET Core Identity)
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.SlidingExpiration = true;
        });

        // JWT Authentication (for API scenarios)
        var jwtSection = configuration.GetSection("Jwt");
        if (jwtSection.Exists())
        {
            services.AddAuthentication()
                .AddJwtBearer("JWT", options =>
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

        // Add Authorization
        services.AddAuthorization(options =>
        {
            // Default policies
            options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("SuperAdmin", "Admin"));
            options.AddPolicy("RequireUserRole", policy => policy.RequireRole("SuperAdmin", "Admin", "User"));
        });
    }

    /// <summary>
    /// Seeds default data (roles, admin user)
    /// </summary>
    public static async Task<IServiceProvider> SeedAspNetIdentityDataAsync(this IServiceProvider serviceProvider, string adminEmail = "admin@example.com", string adminPassword = "Admin123!")
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AspNetIdentityDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Entities.IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Entities.IdentityRole>>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Create roles if they don't exist
        string[] roles = { "SuperAdmin", "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var identityRole = new Entities.IdentityRole(role)
                {
                    Id = Guid.NewGuid(),
                    Description = role switch
                    {
                        "SuperAdmin" => "System administrator with full access",
                        "Admin" => "Application administrator",
                        "User" => "Standard user",
                        _ => $"{role} role"
                    },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await roleManager.CreateAsync(identityRole);
            }
        }

        // Create admin user if doesn't exist
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new Entities.IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                // Add to SuperAdmin role
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
            }
        }

        return serviceProvider;
    }

    /// <summary>
    /// Configures CORS for AspNet Identity API
    /// </summary>
    public static IServiceCollection AddAspNetIdentityCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" };

        services.AddCors(options =>
        {
            options.AddPolicy("AspNetIdentityPolicy", policy =>
            {
                policy.WithOrigins(corsOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Adds data protection for AspNet Identity
    /// </summary>
    public static IServiceCollection AddAspNetIdentityDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        var dataProtectionSection = configuration.GetSection("DataProtection");
        
        if (dataProtectionSection.Exists())
        {
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionSection["KeysDirectory"] ?? "./keys"))
                .SetApplicationName(dataProtectionSection["ApplicationName"] ?? "DiiwoIdentity");
        }
        else
        {
            services.AddDataProtection();
        }

        return services;
    }

    /// <summary>
    /// Default Identity options
    /// </summary>
    private static void DefaultIdentityOptions(Microsoft.AspNetCore.Identity.IdentityOptions options)
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 1;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = true;

        // Sign in settings
        options.SignIn.RequireConfirmedEmail = true;
        options.SignIn.RequireConfirmedPhoneNumber = false;
    }
}