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

// 2. Database Configuration - SQLite فقط بدون تضارب
var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "fooddelivery.db");

// تأكد من إنشاء مجلد Data
var dataDir = Path.GetDirectoryName(dbPath);
if (!Directory.Exists(dataDir))
{
    Directory.CreateDirectory(dataDir);
}

// إضافة DbContext مرة واحدة فقط
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// 3. Register Services
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// 4. تهيئة قاعدة البيانات - مرة واحدة فقط
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        // إنشاء قاعدة البيانات والجداول
        dbContext.Database.EnsureCreated();
        Console.WriteLine("✅ Database created successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database initialization failed: {ex.Message}");
    }
}

// 5. Swagger Middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Food Delivery API v1");
    c.RoutePrefix = "swagger";
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

app.Run();