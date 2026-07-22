using System.Reflection;
using DotNetEnv;
using Npgsql;

// Load local .env for development (never commit real secrets).
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "CapTap API",
        Version = "v1",
        Description = "CapTap medication adherence API"
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin());
});

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CapTap API v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapGet("/health", async () =>
{
    var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION")
        ?? builder.Configuration.GetConnectionString("DefaultConnection");

    var databaseStatus = "unknown";

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync();
            databaseStatus = "connected";
        }
        catch (Exception)
        {
            databaseStatus = "unavailable";
        }
    }
    else
    {
        databaseStatus = "not_configured";
    }

    var status = databaseStatus == "connected" ? "healthy" : "degraded";

    return Results.Ok(new
    {
        status,
        database = databaseStatus,
        version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"
    });
})
.WithName("Health")
.WithTags("System");

app.Run();
