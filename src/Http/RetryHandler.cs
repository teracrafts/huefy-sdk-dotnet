namespace Huefy.Sdk.Http;

using Huefy.Sdk.Errors;

/// <summary>
/// Retry handler with exponential backoff and jitter.
/// </summary>
public sealed class RetryHandler
{
    private readonly int _maxRetries;
    private readonly int _initialDelayMs;
    private readonly int _maxDelayMs;
    private readonly double _backoffMultiplier;
    private readonly int _jitterMs;

    public RetryHandler(RetryConfig config)
    {
        _maxRetries = config.MaxRetries;
        _initialDelayMs = config.InitialDelayMs;
        _maxDelayMs = config.MaxDelayMs;
        _backoffMultiplier = config.BackoffMultiplier;
        _jitterMs = config.JitterMs;
    }

    /// <summary>
    /// Executes the given async operation with retry logic.
    /// Only retries on recoverable errors.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        HuefyException? lastException = null;

        for (var attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (HuefyException ex) when (ex.Recoverable && attempt < _maxRetries)
            {
                lastException = ex;
                var delay = CalculateDelay(attempt, ex.RetryAfter);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (attempt < _maxRetries)
            {
                lastException = HuefyException.NetworkError(ex.Message, ex);
                var delay = CalculateDelay(attempt, retryAfter: null);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < _maxRetries)
            {
                lastException = HuefyException.TimeoutError("Request timed out", ex);
                var delay = CalculateDelay(attempt, retryAfter: null);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastException ?? HuefyException.NetworkError("Operation failed after all retry attempts");
    }

    private int CalculateDelay(int attempt, long? retryAfter)
    {
        if (retryAfter.HasValue && retryAfter.Value > 0)
            return (int)Math.Min(retryAfter.Value, _maxDelayMs);

        var exponentialDelay = _initialDelayMs * Math.Pow(_backoffMultiplier, attempt);
        var jitter = Random.Shared.Next(0, _jitterMs);
        var totalDelay = (int)Math.Min(exponentialDelay + jitter, _maxDelayMs);

        return totalDelay;
    }
}
