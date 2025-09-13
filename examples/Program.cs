using Diiwo.Identity.Shared.Extensions;

// CLI commands - Process before starting the app
if (args.ProcessPermissionCommands())
{
    return; // Exit after processing CLI command
}

// Normal application startup
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add your identity architecture
// builder.Services.AddAppIdentity();     // For simple projects
// builder.Services.AddAspNetIdentity();  // For enterprise projects

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Optional: Runtime permission generation (for development)
await app.GeneratePermissionsAsync();

app.UseRouting();
app.MapControllers();

app.Run();

// Example entities with permissions (in your domain layer)
/*
using Diiwo.Identity.Shared.Attributes;

[Permission("View", "View customer information")]
[Permission("Create", "Create new customers")]
[Permission("Edit", "Edit customer details")]
[Permission("Delete", "Delete customers")]
public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

[Permission("View", "View products")]
[Permission("Create", "Add new products")]
[Permission("ManageInventory", "Manage stock levels", PermissionScope.Global, 50)]
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
*/