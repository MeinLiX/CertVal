using CertVal.Application.Common.Exceptions;
using CertVal.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using ApplicationException = CertVal.Application.Common.Exceptions.ApplicationException;

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
            _logger.LogError(ex, "An unexpected error occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        ErrorResponseDto errorResponse;

        switch (exception)
        {
            case ValidationException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = new ErrorResponseDto("Validation failed", new Dictionary<string, string[]>(ex.Errors));
                break;

            case NotFoundException ex:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse = new ErrorResponseDto(ex.Message);
                break;

            case UnauthorizedException ex:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse = new ErrorResponseDto(ex.Message);
                break;

            case ForbiddenException ex:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                errorResponse = new ErrorResponseDto(ex.Message);
                break;

            case ApplicationException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = new ErrorResponseDto(ex.Message);
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse = new ErrorResponseDto("An internal server error occurred");
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }
}