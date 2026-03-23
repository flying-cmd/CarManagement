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
}

public sealed class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message = "Unauthorized") : base(message, StatusCodes.Status401Unauthorized)
    {
    }
}

public sealed class ForbiddenException : ApiException
{
    public ForbiddenException(string message = "Forbidden.") : base(message, StatusCodes.Status403Forbidden)
    {
    }
}

public sealed class NotFoundException : ApiException
{
    public NotFoundException(string message = "Resource not found.") : base(message, StatusCodes.Status404NotFound)
    {
    }
}

public sealed class ConflictException : ApiException
{
    public ConflictException(string message = "A conflict occurred.") : base(message, StatusCodes.Status409Conflict)
    {
    }
}
