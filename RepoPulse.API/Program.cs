using Microsoft.EntityFrameworkCore;
using RepoPulse.API.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Data Initialisation
// Execute custom startup logic to apply EF Core migrations
// Pass in app.Services so the initialiser can resolve the DbContext

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
app.UseAuthorization();
app.MapControllers();

app.Run();
