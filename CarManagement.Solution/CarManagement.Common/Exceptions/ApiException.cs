using Microsoft.AspNetCore.Http;

namespace CarManagement.Common.Exceptions;

/// <summary>
/// Custome application exception used to return consistent API error responses.
/// Stores the HTTP status code and error details.
/// </summary>
public class ApiException : Exception
{
    public int StatusCode { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }

    /// <summary>
    /// Creates a new instance of the <see cref="ApiException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errors">Optional dictionary of detailed errors.</param>
    /// <param name="innerException">Optional inner exception.</param>
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

    /// <summary>
    /// Creates a 400 Bad Request exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errors">Optional dictionary of detailed errors.</param>
    /// <returns>Returns a new <see cref="ApiException"/> instance with status code 400.</returns>
    public static ApiException BadRequest(
        string message = "Bad request.",
        IDictionary<string, string[]>? errors = null)
        => new(message, StatusCodes.Status400BadRequest, errors);

    /// <summary>
    /// Creates a 401 Unauthorized exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>Returns a new <see cref="ApiException"/> instance with status code 401.</returns>
    public static ApiException Unauthorized(
        string message = "Unauthorized.")
        => new(message, StatusCodes.Status401Unauthorized);

    /// <summary>
    /// Creates a 403 Forbidden exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>Returns a new <see cref="ApiException"/> instance with status code 403.</returns>
    public static ApiException Forbidden(
        string message = "Forbidden.")
        => new(message, StatusCodes.Status403Forbidden);

    /// <summary>
    /// Creates a 404 Not Found exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>Returns a new <see cref="ApiException"/> instance with status code 404.</returns>
    public static ApiException NotFound(
        string message = "Resource not found.")
        => new(message, StatusCodes.Status404NotFound);

    /// <summary>
    /// Creates a 409 Conflict exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>Returns a new <see cref="ApiException"/> instance with status code 409.</returns>
    public static ApiException Conflict(
        string message = "A conflict occurred.")
        => new(message, StatusCodes.Status409Conflict);

    /// <summary>
    /// Creates a 500 Internal Server Error exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>Returns a new <see cref="ApiException"/> instance with status code 500.</returns>
    public static ApiException InternalServerError(
        string message = "An unexpected error occurred.")
        => new(message, StatusCodes.Status500InternalServerError);
}
