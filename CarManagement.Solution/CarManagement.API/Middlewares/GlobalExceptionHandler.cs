using CarManagement.Common.Exceptions;
using CarManagement.Common.Helpers;
using FluentValidation;

namespace CarManagement.API.Middlewares;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _hostEnvironment;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger, IHostEnvironment hostEnvironment)
    {
        _next = next;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception occurred for {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            // Build error response
            var (statusCode, response) = BuidErrorResponse(context, exception);
        
            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(response);
        }
    }

    /// <summary>
    /// Map exception to error response
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="exception">The exception.</param>
    /// <returns>Returns (int StatusCode, ApiResponse<object?> Response).</returns>
    private (int StatusCode, ApiResponse<object?> Response) BuidErrorResponse(HttpContext context, Exception exception)
    {
        return exception switch
        {
            ApiException apiException => (
                apiException.StatusCode,
                ApiResponse<object?>.FailureResponse(
                    apiException.Message,
                    apiException.StatusCode,
                    NormalizeErrors(apiException.Errors, apiException.Message),
                    context.TraceIdentifier)),

            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                ApiResponse<object?>.FailureResponse(
                    "Validation failed.",
                    StatusCodes.Status400BadRequest,
                    validationException.Errors
                        .GroupBy(error => string.IsNullOrWhiteSpace(error.PropertyName) ? "Error" : error.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(error => error.ErrorMessage).Distinct().ToArray()),
                    context.TraceIdentifier)),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                ApiResponse<object?>.FailureResponse(
                    "Unauthorized access.",
                    StatusCodes.Status401Unauthorized,
                    CreateSingleError("Error", "Unauthorized access."),
                    context.TraceIdentifier)),

            ArgumentOutOfRangeException argumentOutOfRangeException => (
                StatusCodes.Status400BadRequest,
                ApiResponse<object?>.FailureResponse(
                    argumentOutOfRangeException.Message,
                    StatusCodes.Status400BadRequest,
                    CreateSingleError("Error", argumentOutOfRangeException.Message),
                    context.TraceIdentifier)),

            ArgumentException argumentException => (
                StatusCodes.Status400BadRequest,
                ApiResponse<object?>.FailureResponse(
                    argumentException.Message,
                    StatusCodes.Status400BadRequest,
                    CreateSingleError("Error", argumentException.Message),
                    context.TraceIdentifier)),

            KeyNotFoundException keyNotFoundException => (
                StatusCodes.Status404NotFound,
                ApiResponse<object?>.FailureResponse(
                    keyNotFoundException.Message,
                    StatusCodes.Status404NotFound,
                    CreateSingleError("Error", keyNotFoundException.Message),
                    context.TraceIdentifier)),

            _ => (
                StatusCodes.Status500InternalServerError,
                ApiResponse<object?>.FailureResponse(
                    _hostEnvironment.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                    StatusCodes.Status500InternalServerError,
                    CreateSingleError(
                        "Error",
                        _hostEnvironment.IsDevelopment() ? exception.Message : "An unexpected error occurred."),
                    context.TraceIdentifier))
        };
    }

    /// <summary>
    /// Normalize errors. If 'errors' is not null and contains more than one error, return it. Otherwise, create a single error.
    /// </summary>
    /// <param name="errors">The errors.</param>
    /// <param name="errorMessage">A single error message.</param>
    /// <returns>Returns the normalized errors which is a dictionary.</returns>
    private static IDictionary<string, string[]> NormalizeErrors(IDictionary<string, string[]>? errors, string errorMessage)
    {
        return errors?.Count > 0 ? errors : CreateSingleError("Error", errorMessage);
    }

    /// <summary>
    /// Create a one item error dictionary.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="message">A single error message.</param>
    /// <returns>Returns the one item error dictionary.</returns>
    private static IDictionary<string, string[]> CreateSingleError(string key, string message)
    {
        return new Dictionary<string, string[]> { { key, new[] { message } } };
    }
}
