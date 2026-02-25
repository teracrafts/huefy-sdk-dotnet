namespace Huefy.Sdk.Http;

using Huefy.Sdk.Errors;

/// <summary>
/// Circuit breaker states.
/// </summary>
public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}

/// <summary>
/// Thread-safe circuit breaker implementation with automatic state transitions.
/// </summary>
public sealed class CircuitBreaker
{
    private readonly object _lock = new();
    private readonly int _failureThreshold;
    private readonly int _resetTimeoutMs;
    private readonly int _halfOpenMaxAttempts;

    private CircuitState _state = CircuitState.Closed;
    private int _failureCount;
    private int _halfOpenSuccessCount;
    private DateTimeOffset _lastFailureTime = DateTimeOffset.MinValue;

    /// <summary>Current state of the circuit breaker.</summary>
    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                if (_state == CircuitState.Open && ShouldTransitionToHalfOpen())
                {
                    _state = CircuitState.HalfOpen;
                    _halfOpenSuccessCount = 0;
                }
                return _state;
            }
        }
    }

    /// <summary>Current consecutive failure count.</summary>
    public int FailureCount
    {
        get { lock (_lock) { return _failureCount; } }
    }

    public CircuitBreaker(CircuitBreakerConfig config)
    {
        _failureThreshold = config.FailureThreshold;
        _resetTimeoutMs = config.ResetTimeoutMs;
        _halfOpenMaxAttempts = config.HalfOpenMaxAttempts;
    }

    /// <summary>
    /// Executes the given async operation through the circuit breaker.
    /// Throws <see cref="HuefyException"/> if the circuit is open.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        EnsureCircuitAllowsRequest();

        try
        {
            var result = await operation().ConfigureAwait(false);
            RecordSuccess();
            return result;
        }
        catch (HuefyException ex) when (ex.Recoverable)
        {
            RecordFailure();
            throw;
        }
        catch (HttpRequestException)
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Records a successful request, potentially closing the circuit.
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            if (_state == CircuitState.HalfOpen)
            {
                _halfOpenSuccessCount++;
                if (_halfOpenSuccessCount >= _halfOpenMaxAttempts)
                {
                    _state = CircuitState.Closed;
                    _failureCount = 0;
                    _halfOpenSuccessCount = 0;
                }
            }
            else if (_state == CircuitState.Closed)
            {
                _failureCount = 0;
            }
        }
    }

    /// <summary>
    /// Records a failed request, potentially opening the circuit.
    /// </summary>
    public void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTimeOffset.UtcNow;

            if (_state == CircuitState.HalfOpen)
            {
                _state = CircuitState.Open;
                _halfOpenSuccessCount = 0;
            }
            else if (_state == CircuitState.Closed && _failureCount >= _failureThreshold)
            {
                _state = CircuitState.Open;
            }
        }
    }

    /// <summary>
    /// Resets the circuit breaker to the closed state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitState.Closed;
            _failureCount = 0;
            _halfOpenSuccessCount = 0;
        }
    }

    private void EnsureCircuitAllowsRequest()
    {
        lock (_lock)
        {
            if (_state == CircuitState.Open)
            {
                if (ShouldTransitionToHalfOpen())
                {
                    _state = CircuitState.HalfOpen;
                    _halfOpenSuccessCount = 0;
                }
                else
                {
                    throw HuefyException.CircuitBreakerOpen(
                        $"Circuit breaker is open. {_failureCount} consecutive failures recorded. " +
                        $"Will retry after {_resetTimeoutMs}ms.");
                }
            }
        }
    }

    private bool ShouldTransitionToHalfOpen() =>
        (DateTimeOffset.UtcNow - _lastFailureTime).TotalMilliseconds >= _resetTimeoutMs;
}
