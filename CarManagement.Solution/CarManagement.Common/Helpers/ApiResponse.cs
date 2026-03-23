using Microsoft.AspNetCore.Http;

namespace CarManagement.Common.Helpers;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }
    public string? TraceId { get; init; }

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
