using Api.Data;
using Api.Data.Context;
using Microsoft.EntityFrameworkCore;




var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure JSON options to handle cycles
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

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

// *** ADD CORS Policy ***
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins"; // Define a policy name

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy  =>
                      {
                          policy.WithOrigins("https://Tanner253.github.io") // Allow your GitHub Pages origin
                                .AllowAnyHeader() // Allow common headers like Content-Type, Authorization
                                .AllowAnyMethod(); // Allow GET, POST, PUT, DELETE etc.
                          // Consider policy.AllowCredentials() if you need cookies/auth headers with CORS
                      });
});
// ************************

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();      // Serves the raw Swagger JSON definition
    app.UseSwaggerUI();    // Serves the interactive Swagger UI HTML page
}

// Comment out HTTPS redirection for local HTTP testing
// app.UseHttpsRedirection();

app.UseRouting(); // Ensure UseRouting is called before UseCors

// *** USE CORS Policy ***
// IMPORTANT: Add UseCors BEFORE UseAuthorization and MapControllers
app.UseCors(MyAllowSpecificOrigins);
// ***********************

app.UseAuthorization();

app.MapControllers();

app.Run();
