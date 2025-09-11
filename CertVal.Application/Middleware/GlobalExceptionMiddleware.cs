using CertVal.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace CertVal.Application.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case ValidationException validationEx:
                response.Message = "Validation failed";
                response.Errors = validationEx.Errors.ToDictionary(e => e.PropertyName, e => e.ErrorMessage);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case NotFoundException:
                response.Message = exception.Message;
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                break;

            case UnauthorizedException:
                response.Message = exception.Message;
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            case ForbiddenException:
                response.Message = exception.Message;
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                break;

            default:
                response.Message = "An internal server error occurred";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string>? Errors { get; set; }
}