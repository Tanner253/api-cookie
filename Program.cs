using Api.Data;
using Api.Data.Context;
using Microsoft.EntityFrameworkCore;




var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();

/// <summary>
///  Sets up Dependency Injection for the DbContext
/// </summary>
//Setup DbConnection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (String.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string not found");
}
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddEndpointsApiExplorer(); // Needed by Swagger
builder.Services.AddSwaggerGen();          // Configures Swagger generation


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();      // Serves the raw Swagger JSON definition
    app.UseSwaggerUI();    // Serves the interactive Swagger UI HTML page
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
