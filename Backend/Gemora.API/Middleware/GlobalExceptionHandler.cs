using System.Net;
using System.Text.Json;

namespace Gemora.API.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        RequestDelegate next,
        ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException ex)
        {
            // Expected business-rule violation
            _logger.LogWarning(
                ex,
                "Business rule violation: {Message}",
                ex.Message);

            await WriteErrorResponse(
                context,
                HttpStatusCode.Conflict,
                ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                ex,
                "Unauthorized operation: {Message}",
                ex.Message);

            await WriteErrorResponse(
                context,
                HttpStatusCode.Forbidden,
                ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Resource not found: {Message}",
                ex.Message);

            await WriteErrorResponse(
                context,
                HttpStatusCode.NotFound,
                ex.Message);
        }
        catch (Exception ex)
        {
            // Real unexpected server error
            _logger.LogError(
                ex,
                "Unhandled exception occurred.");

            await WriteErrorResponse(
                context,
                HttpStatusCode.InternalServerError,
                "An unexpected server error occurred.");
        }
    }

    private static async Task WriteErrorResponse(
        HttpContext context,
        HttpStatusCode statusCode,
        string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = (int)statusCode,
            error = statusCode.ToString(),
            message
        };

        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }
}