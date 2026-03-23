using Microsoft.AspNetCore.Http;

namespace CarManagement.Common.Helpers;

public class ApiException : Exception
{
    public int StatusCode { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }

    public ApiException(
        string message,
        int statusCode,
        IDictionary<string, string[]>? errors = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public static ApiException BadRequest(
        string message = "Bad request.",
        IDictionary<string, string[]>? errors = null)
        => new(message, StatusCodes.Status400BadRequest, errors);

    public static ApiException Unauthorized(
        string message = "Unauthorized.")
        => new(message, StatusCodes.Status401Unauthorized);

    public static ApiException Forbidden(
        string message = "Forbidden.")
        => new(message, StatusCodes.Status403Forbidden);

    public static ApiException NotFound(
        string message = "Resource not found.")
        => new(message, StatusCodes.Status404NotFound);

    public static ApiException Conflict(
        string message = "A conflict occurred.")
        => new(message, StatusCodes.Status409Conflict);
}
