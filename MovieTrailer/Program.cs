using AspNetCoreRateLimit;
using MovieTrailer.Extensions;
using MovieTrailer.Middleware;
using MovieTrailer.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddStructuredLogging();
builder.Services.AddApplicationServices();
builder.Services.AddCaching(builder.Configuration);
builder.Services.AddExternalClients(builder.Configuration);
builder.Services.AddApiCors(builder.Configuration);
builder.Services.AddRateLimiting(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseSerilogRequestLogging();
app.UseIpRateLimiting();
app.UseCors("Frontend");
app.UseRouting();
app.MapControllers();
app.MapGet("/health", (IWebHostEnvironment env) =>
    Results.Ok(new HealthResponse("healthy", DateTime.UtcNow, env.EnvironmentName)));

app.Run();
