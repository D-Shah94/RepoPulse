using Microsoft.EntityFrameworkCore;
using RepoPulse.API.Data;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Database configuration (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Controllers & JSON formatting
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Prevent infinite loops when serialising EF Core relational models
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Swagger/OpenAPI configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Data Initialisation

try
{
    await DatabaseInitialiser.InitialiseAsync(app.Services);
}
catch (Exception ex)
{
    // If the database fails to initialise, it's caught here at the top level
    // and stops the application completely
    Console.WriteLine($"Fatal error during database initialisation: {ex.Message}");
    return; // exit app
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


// Enable CORS policy defined above (before authorisation)
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
