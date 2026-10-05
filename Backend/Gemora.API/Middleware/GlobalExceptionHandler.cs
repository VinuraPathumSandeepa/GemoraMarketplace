using System.Net;
using System.Text.Json;

namespace Gemora.API.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;

    private readonly ILogger<GlobalExceptionHandler>
        _logger;

    private readonly IWebHostEnvironment
        _environment;


    public GlobalExceptionHandler(
        RequestDelegate next,
        ILogger<GlobalExceptionHandler> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }


    public async Task InvokeAsync(
        HttpContext context)
    {
        try
        {
            await _next(context);
        }

        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Business rule violation: {Message}",
                ex.Message);

            await WriteErrorResponse(
                context,
                HttpStatusCode.Conflict,
                ex.Message,
                ex);
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
                ex.Message,
                ex);
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
                ex.Message,
                ex);
        }

        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception occurred while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            var message =
                _environment.IsDevelopment()
                    ? ex.Message
                    : "An unexpected server error occurred.";

            await WriteErrorResponse(
                context,
                HttpStatusCode.InternalServerError,
                message,
                ex);
        }
    }


    private async Task WriteErrorResponse(
        HttpContext context,
        HttpStatusCode statusCode,
        string message,
        Exception exception)
    {
        if (context.Response.HasStarted)
        {
            return;
        }


        context.Response.Clear();

        context.Response.StatusCode =
            (int)statusCode;

        context.Response.ContentType =
            "application/json";


        object response;


        if (_environment.IsDevelopment())
        {
            response = new
            {
                status =
                    (int)statusCode,

                error =
                    statusCode.ToString(),

                message,

                exceptionType =
                    exception.GetType().FullName,

                detail =
                    exception.ToString(),

                innerException =
                    exception.InnerException?.Message,

                path =
                    context.Request.Path.Value
            };
        }
        else
        {
            response = new
            {
                status =
                    (int)statusCode,

                error =
                    statusCode.ToString(),

                message
            };
        }


        var json =
            JsonSerializer.Serialize(
                response,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy =
                        JsonNamingPolicy.CamelCase
                });


        await context.Response
            .WriteAsync(json);
    }
}