# Teracrafts.Huefy

Official .NET SDK for [Huefy](https://huefy.dev) — transactional email delivery made simple.

## Installation

```bash
dotnet add package Teracrafts.Huefy
```

Or via NuGet Package Manager:

```powershell
Install-Package Teracrafts.Huefy
```

## Requirements

- .NET 6+

## Quick Start

```csharp
using Teracrafts.Huefy;

var client = new HuefyEmailClient(new HuefyOptions
{
    ApiKey = Environment.GetEnvironmentVariable("HUEFY_API_KEY")!,
});

var response = await client.SendEmailAsync(new SendEmailRequest
{
    TemplateKey = "welcome-email",
    Recipient = new Recipient { Email = "alice@example.com", Name = "Alice" },
    Variables = new Dictionary<string, object>
    {
        ["firstName"] = "Alice",
        ["trialDays"] = 14,
    },
});

Console.WriteLine($"Message ID: {response.MessageId}");
client.Dispose();
```

## ASP.NET Core / Dependency Injection

```csharp
// Program.cs
builder.Services.AddHuefy(options =>
{
    options.ApiKey = builder.Configuration["Huefy:ApiKey"]!;
});
```

This registers `IHuefyEmailClient` as a singleton and wires up `IHttpClientFactory` for optimal connection reuse.

```csharp
// MyService.cs
public class MyService(IHuefyEmailClient huefy)
{
    public async Task SendWelcomeAsync(string email)
    {
        await huefy.SendEmailAsync(new SendEmailRequest
        {
            TemplateKey = "welcome-email",
            Recipient = new Recipient { Email = email },
        });
    }
}
```

## Key Features

- **Fully async** — all methods return `Task<T>` and accept an optional `CancellationToken`
- **`IDisposable` / `IAsyncDisposable`** — clean resource management
- **`IHttpClientFactory` compatible** — integrates with ASP.NET Core DI for connection pooling
- **`ILogger` support** — plug in any `Microsoft.Extensions.Logging` provider
- **PascalCase conventions** — follows .NET naming throughout
- **Retry with exponential backoff** — configurable attempts, base delay, ceiling, and jitter
- **Circuit breaker** — opens after 5 consecutive failures, probes after 30 s
- **HMAC-SHA256 signing** — optional request signing for additional integrity verification
- **Key rotation** — primary + secondary API key with seamless failover
- **Rate limit callbacks** — `OnRateLimitUpdate` fires whenever rate-limit headers change
- **PII detection** — warns when template variables contain sensitive field patterns

## Configuration Reference

| Option | Default | Description |
|--------|---------|-------------|
| `ApiKey` | — | **Required.** Must have prefix `sdk_`, `srv_`, or `cli_` |
| `BaseUrl` | `https://api.huefy.dev/api/v1/sdk` | Override the API base URL |
| `Timeout` | `TimeSpan.FromSeconds(30)` | Request timeout |
| `RetryOptions.MaxAttempts` | `3` | Total attempts including the first |
| `RetryOptions.BaseDelay` | `500ms` | Exponential backoff base delay |
| `RetryOptions.MaxDelay` | `10s` | Maximum backoff delay |
| `RetryOptions.Jitter` | `0.2` | Random jitter factor (0–1) |
| `CircuitBreakerOptions.FailureThreshold` | `5` | Consecutive failures before circuit opens |
| `CircuitBreakerOptions.ResetTimeout` | `30s` | Duration before half-open probe |
| `SecondaryApiKey` | `null` | Backup key used during key rotation |
| `EnableRequestSigning` | `false` | Enable HMAC-SHA256 request signing |
| `OnRateLimitUpdate` | `null` | Action fired on rate-limit header changes |

## Bulk Email

```csharp
var bulk = await client.SendBulkEmailsAsync(new BulkEmailRequest
{
    Emails =
    [
        new SendEmailRequest { TemplateKey = "promo", Recipient = new Recipient { Email = "bob@example.com" } },
        new SendEmailRequest { TemplateKey = "promo", Recipient = new Recipient { Email = "carol@example.com" } },
    ],
}, cancellationToken);

Console.WriteLine($"Sent: {bulk.TotalSent}, Failed: {bulk.TotalFailed}");
```

## Error Handling

```csharp
using Teracrafts.Huefy.Exceptions;

try
{
    var response = await client.SendEmailAsync(request, cancellationToken);
    Console.WriteLine($"Delivered: {response.MessageId}");
}
catch (HuefyAuthException)
{
    Console.Error.WriteLine("Invalid API key");
}
catch (HuefyRateLimitException e)
{
    Console.Error.WriteLine($"Rate limited. Retry after {e.RetryAfter}s");
}
catch (HuefyCircuitOpenException)
{
    Console.Error.WriteLine("Circuit open — service unavailable, backing off");
}
catch (HuefyException e)
{
    Console.Error.WriteLine($"Huefy error [{e.Code}]: {e.Message}");
}
```

### Error Code Reference

| Exception | Code | Meaning |
|-----------|------|---------|
| `HuefyInitException` | 1001 | Client failed to initialise |
| `HuefyAuthException` | 1102 | API key rejected |
| `HuefyNetworkException` | 1201 | Upstream request failed |
| `HuefyCircuitOpenException` | 1301 | Circuit breaker tripped |
| `HuefyRateLimitException` | 2003 | Rate limit exceeded |
| `HuefyTemplateMissingException` | 2005 | Template key not found |

## Health Check

```csharp
var health = await client.HealthCheckAsync();
if (health.Status != "healthy")
{
    logger.LogWarning("Huefy degraded: {Status}", health.Status);
}
```

## Local Development

Set `HUEFY_MODE=local` to point the SDK at a local Huefy server, or override `BaseUrl` in options:

```csharp
var client = new HuefyEmailClient(new HuefyOptions
{
    ApiKey = "sdk_local_key",
    BaseUrl = new Uri("http://localhost:3000/api/v1/sdk"),
});
```

Or via `appsettings.Development.json`:

```json
{
  "Huefy": {
    "ApiKey": "sdk_local_key",
    "BaseUrl": "http://localhost:3000/api/v1/sdk"
  }
}
```

## Developer Guide

Full documentation, advanced patterns, and provider configuration are in the [.NET Developer Guide](../../docs/spec/guides/dotnet.guide.md).

## License

MIT
