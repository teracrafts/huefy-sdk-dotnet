namespace Teracrafts.Huefy.Sdk.Errors;

/// <summary>
/// Enumeration of all SDK error codes.
/// </summary>
public enum ErrorCode
{
    // Network errors (1xxx)
    NetworkError = 1000,
    ConnectionRefused = 1001,
    DnsResolutionFailed = 1002,
    Timeout = 1003,
    SslError = 1004,

    // Authentication errors (2xxx)
    AuthenticationFailed = 2000,
    InvalidApiKey = 2001,
    ExpiredApiKey = 2002,
    InsufficientPermissions = 2003,
    KeyRotationFailed = 2004,

    // Request errors (3xxx)
    ValidationError = 3000,
    InvalidPayload = 3001,
    MissingRequiredField = 3002,
    PayloadTooLarge = 3003,
    UnsupportedMediaType = 3004,

    // Server errors (4xxx)
    ServerError = 4000,
    ServiceUnavailable = 4001,
    BadGateway = 4002,
    GatewayTimeout = 4003,
    InternalServerError = 4004,

    // Rate limiting (5xxx)
    RateLimited = 5000,
    QuotaExceeded = 5001,
    ConcurrencyLimitExceeded = 5002,

    // Circuit breaker (6xxx)
    CircuitBreakerOpen = 6000,
    CircuitBreakerHalfOpen = 6001,

    // Security errors (7xxx)
    SecurityError = 7000,
    SignatureVerificationFailed = 7001,
    PiiDetected = 7002,
    SanitizationFailed = 7003,

    // Unknown
    Unknown = 9999
}
