using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        _next =
            next;

        _logger =
            logger;

        _environment =
            environment;
    }


    public async Task InvokeAsync(
        HttpContext context)
    {
        try
        {
            await _next(
                context);
        }

        // ========================================================
        // EXTERNAL HTTP SERVICE ERROR
        //
        // Examples:
        // - Gemini 429
        // - Gemini 503
        // - Other external HTTP failures
        // ========================================================

        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "External HTTP service error while processing {Method} {Path}: {Message}",
                context.Request.Method,
                context.Request.Path,
                ex.Message
            );


            var statusCode =
                ex.StatusCode ??
                HttpStatusCode
                    .ServiceUnavailable;


            // Do not return unusual/non-server HTTP status codes
            // blindly from external services.

            if (
                statusCode !=
                    HttpStatusCode.RequestTimeout
                &&
                (int)statusCode != 429
                &&
                (int)statusCode < 500
            )
            {
                statusCode =
                    HttpStatusCode.BadGateway;
            }


            await WriteErrorResponse(
                context,
                statusCode,
                ex.Message,
                ex
            );
        }

        // ========================================================
        // BUSINESS RULE ERROR
        // ========================================================

        catch (Exception ex) when (
            ex is PostgresException { SqlState: "40001" or "40P01" } ||
            ex is DbUpdateException { InnerException: PostgresException { SqlState: "40001" or "40P01" } } ||
            ex is DbUpdateException { InnerException: PostgresException { SqlState: "23505", ConstraintName: "IX_Orders_ReservedGem" or "IX_Orders_ActiveBuyerGem" } })
        {
            await WriteErrorResponse(context, HttpStatusCode.Conflict,
                "This order or gemstone changed while you were updating it. Refresh your orders and try again.", ex);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Business rule violation: {Message}",
                ex.Message
            );


            await WriteErrorResponse(
                context,
                HttpStatusCode.Conflict,
                ex.Message,
                ex
            );
        }

        // ========================================================
        // AUTHORIZATION ERROR
        // ========================================================

        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                ex,
                "Unauthorized operation: {Message}",
                ex.Message
            );


            await WriteErrorResponse(
                context,
                HttpStatusCode.Forbidden,
                ex.Message,
                ex
            );
        }

        // ========================================================
        // NOT FOUND
        // ========================================================

        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Resource not found: {Message}",
                ex.Message
            );


            await WriteErrorResponse(
                context,
                HttpStatusCode.NotFound,
                ex.Message,
                ex
            );
        }

        // ========================================================
        // UNKNOWN SERVER ERROR
        // ========================================================

        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception occurred while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path
            );


            var message =
                _environment
                    .IsDevelopment()

                    ? ex.Message

                    : "An unexpected server error occurred.";


            await WriteErrorResponse(
                context,
                HttpStatusCode
                    .InternalServerError,
                message,
                ex
            );
        }
    }


    private async Task WriteErrorResponse(
        HttpContext context,
        HttpStatusCode statusCode,
        string message,
        Exception exception)
    {
        if (
            context.Response
                .HasStarted
        )
        {
            return;
        }


        context.Response
            .Clear();


        context.Response.StatusCode =
            (int)statusCode;


        context.Response.ContentType =
            "application/json";


        object response;


        if (
            _environment
                .IsDevelopment()
        )
        {
            response =
                new
                {
                    status =
                        (int)statusCode,

                    error =
                        statusCode
                            .ToString(),

                    message,

                    exceptionType =
                        exception
                            .GetType()
                            .FullName,

                    detail =
                        exception
                            .ToString(),

                    innerException =
                        exception
                            .InnerException?
                            .Message,

                    path =
                        context.Request
                            .Path
                            .Value
                };
        }
        else
        {
            response =
                new
                {
                    status =
                        (int)statusCode,

                    error =
                        statusCode
                            .ToString(),

                    message
                };
        }


        var json =
            JsonSerializer.Serialize(
                response,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy =
                        JsonNamingPolicy
                            .CamelCase
                }
            );


        await context.Response
            .WriteAsync(
                json);
    }
}
