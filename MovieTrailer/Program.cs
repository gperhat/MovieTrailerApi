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

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("Referrer-Policy", "no-referrer");
    await next();
});

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
app.MapControllers();
app.MapGet("/health", (IWebHostEnvironment env) =>
    Results.Ok(new HealthResponse("healthy", DateTime.UtcNow, env.EnvironmentName)));

app.Run();
