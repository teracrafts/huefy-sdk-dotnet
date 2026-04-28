using Teracrafts.Huefy.Utils;

namespace Teracrafts.Huefy.Sdk;

/// <summary>
/// Parsed rate-limit header values from an API response.
/// </summary>
public record RateLimitInfo(int Limit, int Remaining, DateTimeOffset ResetAt);

/// <summary>
/// Configuration for retry behavior.
/// </summary>
public record RetryConfig
{
    /// <summary>Maximum number of retry attempts.</summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>Initial delay between retries in milliseconds.</summary>
    public int InitialDelayMs { get; init; } = 500;

    /// <summary>Maximum delay between retries in milliseconds.</summary>
    public int MaxDelayMs { get; init; } = 10_000;

    /// <summary>Backoff multiplier applied after each retry.</summary>
    public double BackoffMultiplier { get; init; } = 2.0;
}

/// <summary>
/// Configuration for the circuit breaker.
/// </summary>
public record CircuitBreakerConfig
{
    /// <summary>Number of consecutive failures before the circuit opens.</summary>
    public int FailureThreshold { get; init; } = 5;

    /// <summary>Duration in milliseconds the circuit stays open before transitioning to half-open.</summary>
    public int ResetTimeoutMs { get; init; } = 30_000;

    /// <summary>Number of successful requests in half-open state before closing.</summary>
    public int HalfOpenMaxAttempts { get; init; } = 1;
}

/// <summary>
/// SDK client configuration.
/// </summary>
public record HuefyConfig
{
    /// <summary>Primary API key for authentication.</summary>
    public required string ApiKey { get; init; }

    /// <summary>Base URL for API requests. Resolved from HUEFY_MODE if not set.</summary>
    public string BaseUrl { get; init; } = ResolveBaseUrl();

    /// <summary>Request timeout in milliseconds.</summary>
    public int Timeout { get; init; } = 30_000;

    /// <summary>Retry configuration.</summary>
    public RetryConfig RetryConfig { get; init; } = new();

    /// <summary>Circuit breaker configuration.</summary>
    public CircuitBreakerConfig CircuitBreakerConfig { get; init; } = new();

    /// <summary>Secondary API key used for automatic key rotation on auth failures.</summary>
    public string? SecondaryApiKey { get; init; }

    /// <summary>Enable HMAC-SHA256 request signing.</summary>
    public bool EnableRequestSigning { get; init; }

    /// <summary>Enable sanitization of sensitive data in error messages.</summary>
    public bool EnableErrorSanitization { get; init; } = true;

    /// <summary>Logger instance for SDK diagnostic output. Defaults to null (no logging).</summary>
    public IHuefyLogger? Logger { get; init; }

    /// <summary>Optional callback invoked with rate-limit info after every successful response.</summary>
    public Action<RateLimitInfo>? OnRateLimitUpdate { get; init; }

    /// <summary>Optional callback invoked when remaining requests drop below 20% of the limit.</summary>
    public Action<RateLimitInfo>? OnRateLimitWarning { get; init; }

    private static string ResolveBaseUrl()
    {
        var mode = Environment.GetEnvironmentVariable("HUEFY_MODE");
        return mode?.ToLowerInvariant() switch
        {
            "local" => "https://api.huefy.on/api/v1/sdk",
            _ => "https://api.huefy.dev/api/v1/sdk"
        };
    }
}
