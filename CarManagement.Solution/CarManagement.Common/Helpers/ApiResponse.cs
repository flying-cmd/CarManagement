using Microsoft.AspNetCore.Http;

namespace CarManagement.Common.Helpers;

/// <summary>
/// Standard wrapper for API responses.
/// Provides a consistent structure for API responses.
/// </summary>
/// <typeparam name="T">The type of the response data.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The HTTP status code associated with the response.
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// A message associated with the response.
    /// Usually contains a success or error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// The response data.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// A dictionary of detailed errors.
    /// </summary>
    public IDictionary<string, string[]>? Errors { get; init; }

    /// <summary>
    /// The trace ID used to track the request and debugging.
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// Creates a successful API response.
    /// </summary>
    /// <param name="data">The response data.</param>
    /// <param name="message">The success message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="traceId">The optional trace ID.</param>
    /// <returns>Returns a <see cref="ApiResponse{T}"/>.</returns>
    public static ApiResponse<T> SuccessResponse(
        T data,
        string message = "Success",
        int statusCode = StatusCodes.Status200OK,
        string? traceId = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = statusCode,
            Message = message,
            Data = data,
            TraceId = traceId
        };
    }

    /// <summary>
    /// Creates a failed API response.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errors">A optional dictionary of detailed errors.</param>
    /// <param name="traceId">The optional trace ID.</param>
    /// <returns>Returns a <see cref="ApiResponse{T}"/>.</returns>
    public static ApiResponse<T> FailureResponse(
        string message,
        int statusCode,
        IDictionary<string, string[]>? errors = null,
        string? traceId = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Errors = errors,
            TraceId = traceId
        };
    }
}
