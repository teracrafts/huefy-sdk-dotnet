using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Teracrafts.Huefy.Sdk.Errors;
using Teracrafts.Huefy.Sdk.Security;
using Teracrafts.Huefy.Sdk.Utils;

namespace Teracrafts.Huefy.Sdk.Http;

/// <summary>
/// Internal HTTP client with retry, circuit breaker, key rotation, and HMAC signing.
/// </summary>
internal sealed class SdkHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly RetryHandler _retryHandler;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly HuefyConfig _config;
    private readonly bool _enableSigning;
    private readonly bool _enableSanitization;
    private readonly object _keyLock = new();
    private string _activeApiKey;
    private bool _usingSecondaryKey;

    public SdkHttpClient(HuefyConfig config)
    {
        _config = config;
        _activeApiKey = config.ApiKey;
        _usingSecondaryKey = false;
        _enableSigning = config.EnableRequestSigning;
        _enableSanitization = config.EnableErrorSanitization;
        _httpClient = new HttpClient(
            new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(2) }
        )
        {
            BaseAddress = new Uri(config.BaseUrl),
            Timeout = TimeSpan.FromMilliseconds(config.Timeout),
        };
        _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(SdkVersion.UserAgent);

        _retryHandler = new RetryHandler(config.RetryConfig);
        _circuitBreaker = new CircuitBreaker(config.CircuitBreakerConfig);
    }

    /// <summary>
    /// Sends a GET request through retry and circuit breaker layers.
    /// </summary>
    public Task<T> GetAsync<T>(string path, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Get, path, content: null, ct);

    /// <summary>
    /// Sends a POST request with JSON body through retry and circuit breaker layers.
    /// </summary>
    public Task<T> PostAsync<T>(string path, object payload, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Post, path, payload, ct);

    /// <summary>
    /// Sends a PUT request with JSON body through retry and circuit breaker layers.
    /// </summary>
    public Task<T> PutAsync<T>(string path, object payload, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Put, path, payload, ct);

    /// <summary>
    /// Sends a DELETE request through retry and circuit breaker layers.
    /// </summary>
    public Task<T> DeleteAsync<T>(string path, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Delete, path, content: null, ct);

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? content,
        CancellationToken ct)
    {
        return await _retryHandler.ExecuteAsync(async () =>
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                using var request = BuildRequest(method, path, content);
                HttpResponseMessage response;

                try
                {
                    response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                {
                    throw HuefyException.NetworkError(
                        MaybeSanitize($"Network error: {ex.Message}"), ex);
                }
                catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
                {
                    throw HuefyException.TimeoutError("Request timed out", ex);
                }

                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    ParseRateLimitHeaders(response);
                    return JsonSerializer.Deserialize<T>(body, JsonOptions)
                        ?? throw HuefyException.NetworkError("Failed to deserialize response");
                }

                // Attempt key rotation on 401 if secondary key is available
                if ((int)response.StatusCode == 401 && _config.SecondaryApiKey is not null)
                {
                    lock (_keyLock)
                    {
                        if (!_usingSecondaryKey)
                        {
                            _activeApiKey = _config.SecondaryApiKey;
                            _usingSecondaryKey = true;
                        }
                    }

                    throw HuefyException.KeyRotationError(
                        "Primary key failed, rotating to secondary key");
                }

                var requestId = response.Headers.TryGetValues("X-Request-Id", out var ids)
                    ? ids.FirstOrDefault()
                    : null;

                long? retryAfter = null;
                if (response.Headers.RetryAfter?.Delta.HasValue == true)
                {
                    retryAfter = (long)response.Headers.RetryAfter.Delta.Value.TotalMilliseconds;
                }
                else if (response.Headers.RetryAfter?.Date.HasValue == true)
                {
                    var diff = response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
                    retryAfter = diff.TotalMilliseconds > 0 ? (long)diff.TotalMilliseconds : null;
                }

                throw HuefyException.FromResponse(
                    (int)response.StatusCode,
                    MaybeSanitize(body),
                    requestId,
                    retryAfter);
            }).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string path, object? content)
    {
        string currentKey;
        lock (_keyLock)
        {
            currentKey = _activeApiKey;
        }

        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-API-Key", currentKey);

        string bodyJson = string.Empty;

        if (content is not null)
        {
            bodyJson = JsonSerializer.Serialize(content, JsonOptions);
            request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        }

        if (_enableSigning)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var signingString = SecurityUtils.BuildSigningString(timestamp, bodyJson);
            var signature = SecurityUtils.ComputeHmacSha256(signingString, currentKey);

            request.Headers.Add("X-Timestamp", timestamp.ToString());
            request.Headers.Add("X-Signature", signature);
        }

        return request;
    }

    private void ParseRateLimitHeaders(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("X-RateLimit-Limit", out var limitValues)) return;
        if (!response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues)) return;
        if (!response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues)) return;

        if (!int.TryParse(limitValues.FirstOrDefault(), out var limit)) return;
        if (!int.TryParse(remainingValues.FirstOrDefault(), out var remaining)) return;
        if (!long.TryParse(resetValues.FirstOrDefault(), out var resetUnix)) return;

        var info = new RateLimitInfo(
            Limit: limit,
            Remaining: remaining,
            ResetAt: DateTimeOffset.FromUnixTimeSeconds(resetUnix)
        );

        _config.OnRateLimitUpdate?.Invoke(info);

        if (limit > 0 && remaining < (int)(limit * 0.2))
        {
            _config.OnRateLimitWarning?.Invoke(info);
        }
    }

    private string MaybeSanitize(string input) =>
        _enableSanitization ? ErrorSanitizer.Sanitize(input) : input;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
