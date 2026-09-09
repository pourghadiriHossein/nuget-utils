using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Responses;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Kernel.Middlewares;

/// <summary>
/// A global exception handler that catches unhandled exceptions and formats them into the standard <see cref="ApiResponse{T}"/> JSON format.
/// Logs the exception using standard .NET ILogger (which binds to Serilog).
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // 1. Log the error (Serilog will pick this up)
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        // 2. Prepare the standardized response
        var statusCode = StatusCodes.Status500InternalServerError;
        var message = "An internal server error occurred.";

        if (exception is ArgumentException)
        {
            statusCode = StatusCodes.Status422UnprocessableEntity;
            message = exception.Message;
        }
        else if (exception is KeyNotFoundException)
        {
            statusCode = StatusCodes.Status404NotFound;
            message = exception.Message;
        }
        else if (exception is InvalidOperationException)
        {
            statusCode = StatusCodes.Status409Conflict;
            message = exception.Message;
        }
        else if (exception is UnauthorizedAccessException)
        {
            statusCode = StatusCodes.Status401Unauthorized;
            message = exception.Message;
        }

        object? exceptionDetails = null;

        if (_env.IsDevelopment())
        {
            // Only expose stack trace details in development
            exceptionDetails = new
            {
                Type = exception.GetType().Name,
                exception.Message,
                exception.StackTrace
            };
            message = exception.Message;
        }

        var apiResponse = ApiResponse.Error(statusCode, message, exceptionDetails);

        // 3. Write JSON response
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        await httpContext.Response.WriteAsJsonAsync(apiResponse, jsonOptions, cancellationToken);

        // Return true to indicate the exception has been handled
        return true;
    }
}
