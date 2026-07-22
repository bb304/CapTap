using CapTap.Api.Extensions;
using DotNetEnv;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddCapTapServices(builder.Configuration, builder.Environment)
    .AddSwaggerDocumentation();

var app = builder.Build();

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCapTapMiddleware();
app.UseRateLimiter();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.MapControllers();
app.MapCapTapHealthChecks();

app.Run();

public partial class Program;
