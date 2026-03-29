using System.Net;
using System.Text.Json;
using MovieTrailer.Exceptions;

namespace MovieTrailer.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (MovieNotFoundException ex)
        {
            await RespondWithError(ctx, HttpStatusCode.NotFound, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Upstream HTTP call failed");
            await RespondWithError(ctx, HttpStatusCode.BadGateway, "An upstream service call failed.");
        }
        catch (TaskCanceledException ex)
        {
            logger.LogDebug(ex, "Request cancelled — client disconnected or timeout");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await RespondWithError(ctx, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static Task RespondWithError(HttpContext ctx, HttpStatusCode status, string message)
    {
        ctx.Response.StatusCode = (int)status;
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(new { message, statusCode = (int)status }));
    }
}
