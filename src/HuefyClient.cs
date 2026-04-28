using Teracrafts.Huefy.Sdk.Http;
using Teracrafts.Huefy.Sdk.Models;

namespace Teracrafts.Huefy.Sdk;

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
    public async Task<EmailHealthResponse> HealthCheckAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return await _httpClient.GetAsync<EmailHealthResponse>("/health", ct).ConfigureAwait(false);
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
