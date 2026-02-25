using Huefy.Sdk;
using Huefy.Sdk.Errors;
using Huefy.Sdk.Http;
using Xunit;

namespace Huefy.Sdk.Tests;

public class CircuitBreakerTests
{
    private static CircuitBreaker CreateBreaker(
        int failureThreshold = 3,
        int resetTimeoutMs = 1000,
        int halfOpenMaxAttempts = 2) =>
        new(new CircuitBreakerConfig
        {
            FailureThreshold = failureThreshold,
            ResetTimeoutMs = resetTimeoutMs,
            HalfOpenMaxAttempts = halfOpenMaxAttempts
        });

    [Fact]
    public void InitialState_ShouldBeClosed()
    {
        var breaker = CreateBreaker();
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public void State_ShouldRemainClosed_WhenFailuresBelowThreshold()
    {
        var breaker = CreateBreaker(failureThreshold: 3);

        breaker.RecordFailure();
        breaker.RecordFailure();

        Assert.Equal(CircuitState.Closed, breaker.State);
        Assert.Equal(2, breaker.FailureCount);
    }

    [Fact]
    public void State_ShouldOpen_WhenFailureThresholdReached()
    {
        var breaker = CreateBreaker(failureThreshold: 3);

        breaker.RecordFailure();
        breaker.RecordFailure();
        breaker.RecordFailure();

        Assert.Equal(CircuitState.Open, breaker.State);
    }

    [Fact]
    public async Task Execute_ShouldThrow_WhenCircuitIsOpen()
    {
        var breaker = CreateBreaker(failureThreshold: 1, resetTimeoutMs: 60_000);

        breaker.RecordFailure();

        var ex = await Assert.ThrowsAsync<HuefyException>(
            () => breaker.ExecuteAsync(() => Task.FromResult(42)));

        Assert.Equal(ErrorCode.CircuitBreakerOpen, ex.Code);
        Assert.True(ex.Recoverable);
    }

    [Fact]
    public void FailureCount_ShouldReset_OnSuccess()
    {
        var breaker = CreateBreaker(failureThreshold: 3);

        breaker.RecordFailure();
        breaker.RecordFailure();
        breaker.RecordSuccess();

        Assert.Equal(0, breaker.FailureCount);
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public async Task Execute_ShouldSucceed_WhenCircuitIsClosed()
    {
        var breaker = CreateBreaker();

        var result = await breaker.ExecuteAsync(() => Task.FromResult(42));

        Assert.Equal(42, result);
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public void Reset_ShouldReturnToClosed()
    {
        var breaker = CreateBreaker(failureThreshold: 1);

        breaker.RecordFailure();
        Assert.Equal(CircuitState.Open, breaker.State);

        breaker.Reset();
        Assert.Equal(CircuitState.Closed, breaker.State);
        Assert.Equal(0, breaker.FailureCount);
    }

    [Fact]
    public async Task HalfOpen_ShouldCloseAfterEnoughSuccesses()
    {
        var breaker = CreateBreaker(failureThreshold: 1, resetTimeoutMs: 1, halfOpenMaxAttempts: 2);

        // Open the circuit
        breaker.RecordFailure();
        Assert.Equal(CircuitState.Open, breaker.State);

        // Wait for reset timeout to allow half-open transition
        await Task.Delay(50);

        // Should now be half-open (checked via State property)
        Assert.Equal(CircuitState.HalfOpen, breaker.State);

        // Record enough successes to close
        breaker.RecordSuccess();
        breaker.RecordSuccess();

        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public async Task HalfOpen_ShouldReopenOnFailure()
    {
        var breaker = CreateBreaker(failureThreshold: 1, resetTimeoutMs: 1, halfOpenMaxAttempts: 2);

        breaker.RecordFailure();

        await Task.Delay(50);

        // Transition to half-open
        Assert.Equal(CircuitState.HalfOpen, breaker.State);

        // Failure in half-open should re-open
        breaker.RecordFailure();
        Assert.Equal(CircuitState.Open, breaker.State);
    }

    [Fact]
    public async Task ConcurrentAccess_ShouldBeThreadSafe()
    {
        var breaker = CreateBreaker(failureThreshold: 100);

        var tasks = Enumerable.Range(0, 50).Select(_ =>
            Task.Run(() =>
            {
                breaker.RecordFailure();
                breaker.RecordSuccess();
            }));

        await Task.WhenAll(tasks);

        // Should not throw and state should be consistent
        var state = breaker.State;
        Assert.True(state == CircuitState.Closed || state == CircuitState.Open);
    }
}
