using Huefy.Sdk.Http;

namespace Huefy.Sdk;

/// <summary>
/// Response from the health check endpoint.
/// </summary>
public record HealthResponse
{
    /// <summary>Service health status (e.g., "ok", "degraded").</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Service version string.</summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>Timestamp of the health check response.</summary>
    public long Timestamp { get; init; }
}

/// <summary>
/// Main client for the Huefy SDK.
/// </summary>
public sealed class HuefyClient : IDisposable
{
    private readonly SdkHttpClient _httpClient;
    private bool _disposed;

    /// <summary>
    /// Creates a new SDK client with the given configuration.
    /// </summary>
    public HuefyClient(HuefyConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _httpClient = new SdkHttpClient(config);
    }

    /// <summary>
    /// Performs a health check against the API.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Health check response with status and version.</returns>
    public async Task<HealthResponse> HealthCheckAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return await _httpClient.GetAsync<HealthResponse>("/api/v1/health", ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the client and releases all managed resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
