using System.Security;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fin.Infrastructure.Errors;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.ContentType = "application/json";
            string error;

            switch (ex)
            {
                case UnauthorizedAccessException:
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    error = "You are not authorized to access this resource.";
                    break;
                case SecurityException:
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    error = "You are forbidden to access this resource.";
                    break;
                case KeyNotFoundException:
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    error = "Resources not found.";
                    break;
                default:
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    _logger.LogError(ex, "Unhandled exception");
                    error = "An unexpected error occurred.";
                    break;
            }
            
            var response = new
            {
                error
            };
            
            var json = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
    }
}