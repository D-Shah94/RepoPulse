using Microsoft.EntityFrameworkCore;
using RepoPulse.API.Data;
using RepoPulse.API.Options;
using RepoPulse.API.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

// 2. Configuration & Options
builder.Services.Configure<GitHubApiOptions>(builder.Configuration.GetSection("GitHubApi"));

// 3. Controllers & JSON Formatting
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Prevent infinite loops when serialising EF Core relational models
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddExceptionHandler<RepoPulse.API.Infrastructure.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 4. In-Memory Cache & GitHub Client
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IGitHubService, GitHubService>(client =>
{
    var baseUrl = builder.Configuration["GitHubApi:BaseUrl"] ?? "https://api.github.com";
    var userAgent = builder.Configuration["GitHubApi:UserAgent"] ?? "RepoPulse-App/1.0";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
});

// 4.5. Application Services
builder.Services.AddScoped<IRepositoryService, RepositoryService>();
builder.Services.AddScoped<DependencyParser>();


// 5. CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        policy.WithOrigins("https://localhost:7017", "https://localhost:7001", "http://localhost:5001")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// 6. Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 7. Data Initialisation

try
{
    await DatabaseInitialiser.InitialiseAsync(app.Services);
}
catch (Exception ex)
{
    Console.WriteLine($"Fatal error during database initialisation: {ex.Message}");
    return;
}

// 8. Middleware
app.UseExceptionHandler(); // This will automatically trigger our GlobalExceptionHandler

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();


// Apply the strict Blazor CORS Policy
app.UseCors("BlazorClient");
app.UseAuthorization();
app.MapControllers();

app.Run();
