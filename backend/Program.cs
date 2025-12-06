using FoodDeliveryAPI.Data;
using FoodDeliveryAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 1. Swagger Configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Food Delivery API",
        Version = "v1",
        Description = "API for Food Delivery System"
    });
});

// 2. Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    // Use SQL Server if connection string is provided
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    // Use SQLite with correct path for Render
    var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "fooddelivery.db");

    // Ensure Data directory exists
    var dataDir = Path.GetDirectoryName(dbPath);
    if (!Directory.Exists(dataDir))
    {
        Directory.CreateDirectory(dataDir);
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));
}

// 3. Register Services
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// 4. Apply Migrations and Seed Data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        // Apply migrations or create database
        dbContext.Database.EnsureCreated();
        Console.WriteLine("✅ Database created successfully!");

        // Optional: Seed initial data
        // await SeedData.Initialize(dbContext);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database initialization failed: {ex.Message}");
    }
}

// 5. Swagger Middleware - Enable in Production too!
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Food Delivery API v1");
    c.RoutePrefix = "swagger"; // Access at /swagger
});

// 6. Middleware Pipeline
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// 7. Health Check Endpoint
app.MapGet("/", () =>
{
    return Results.Json(new
    {
        message = "Food Delivery API is running! 🚀",
        status = "active",
        timestamp = DateTime.UtcNow,
        endpoints = new
        {
            restaurants = "/api/restaurants",
            orders = "/api/orders",
            auth = "/api/auth",
            admin = "/api/admin",
            swagger = "/swagger",
            health = "/health"
        }
    });
});

// 8. Health Check Endpoint
app.MapGet("/health", () =>
{
    return Results.Json(new
    {
        status = "healthy",
        service = "Food Delivery API",
        environment = app.Environment.EnvironmentName,
        timestamp = DateTime.UtcNow
    });
});
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // هذا ينشئ قاعدة البيانات والجداول تلقائياً
    dbContext.Database.EnsureCreated();

    Console.WriteLine("✅ Database created successfully!");
}
app.Run();