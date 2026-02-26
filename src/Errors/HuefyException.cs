namespace Huefy.Sdk.Errors;

/// <summary>
/// Base exception for all Huefy SDK errors.
/// </summary>
public class HuefyException : Exception
{
    /// <summary>The structured error code.</summary>
    public ErrorCode Code { get; }

    /// <summary>The numeric error code.</summary>
    public int NumericCode => (int)Code;

    /// <summary>HTTP status code from the response, if available.</summary>
    public int? StatusCode { get; }

    /// <summary>Whether the error is recoverable via retry.</summary>
    public bool Recoverable { get; }

    /// <summary>Suggested retry delay in milliseconds from Retry-After header.</summary>
    public long? RetryAfter { get; }

    /// <summary>Request ID for tracing, if returned by the server.</summary>
    public string? RequestId { get; }

    /// <summary>Unix timestamp in milliseconds when the error occurred.</summary>
    public long Timestamp { get; }

    private HuefyException(
        string message,
        ErrorCode code,
        int? statusCode = null,
        bool recoverable = false,
        long? retryAfter = null,
        string? requestId = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        StatusCode = statusCode;
        Recoverable = recoverable;
        RetryAfter = retryAfter;
        RequestId = requestId;
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Creates a network error exception.
    /// </summary>
    public static HuefyException NetworkError(string message, Exception? inner = null) =>
        new(message, ErrorCode.NetworkError, recoverable: true, innerException: inner);

    /// <summary>
    /// Creates a timeout error exception.
    /// </summary>
    public static HuefyException TimeoutError(string message, Exception? inner = null) =>
        new(message, ErrorCode.Timeout, recoverable: true, innerException: inner);

    /// <summary>
    /// Creates an authentication error exception.
    /// </summary>
    public static HuefyException AuthenticationError(string message, int? statusCode = 401) =>
        new(message, ErrorCode.AuthenticationFailed, statusCode: statusCode, recoverable: false);

    /// <summary>
    /// Creates a recoverable key rotation error exception.
    /// Thrown after the active key has been rotated so the retry handler will
    /// automatically retry the request with the new key.
    /// </summary>
    public static HuefyException KeyRotationError(string message, int? statusCode = 401) =>
        new(message, ErrorCode.KeyRotationFailed, statusCode: statusCode, recoverable: true);

    /// <summary>
    /// Creates a rate-limited error exception.
    /// </summary>
    public static HuefyException RateLimited(string message, long? retryAfter = null) =>
        new(message, ErrorCode.RateLimited, statusCode: 429, recoverable: true, retryAfter: retryAfter);

    /// <summary>
    /// Creates a circuit breaker open exception.
    /// </summary>
    public static HuefyException CircuitBreakerOpen(string message) =>
        new(message, ErrorCode.CircuitBreakerOpen, recoverable: true);

    /// <summary>
    /// Creates a validation error exception.
    /// </summary>
    public static HuefyException ValidationError(string message) =>
        new(message, ErrorCode.ValidationError, statusCode: 400, recoverable: false);

    /// <summary>
    /// Creates a security error exception.
    /// </summary>
    public static HuefyException SecurityError(string message, ErrorCode code = ErrorCode.SecurityError) =>
        new(message, code, recoverable: false);

    /// <summary>
    /// Creates an exception from an HTTP response.
    /// </summary>
    public static HuefyException FromResponse(
        int statusCode,
        string body,
        string? requestId = null,
        long? retryAfter = null)
    {
        var (code, recoverable) = statusCode switch
        {
            400 => (ErrorCode.ValidationError, false),
            401 => (ErrorCode.AuthenticationFailed, false),
            403 => (ErrorCode.InsufficientPermissions, false),
            408 => (ErrorCode.Timeout, true),
            429 => (ErrorCode.RateLimited, true),
            >= 500 and < 600 => (ErrorCode.ServerError, true),
            _ => (ErrorCode.Unknown, false)
        };

        return new HuefyException(
            $"API request failed with status {statusCode}: {body}",
            code,
            statusCode: statusCode,
            recoverable: recoverable,
            retryAfter: retryAfter,
            requestId: requestId);
    }
}
