# Huefy.Sdk

Official .NET SDK for [Huefy](https://huefy.dev) — transactional email delivery made simple.

## Installation

```bash
dotnet add package Huefy.Sdk
```

Or via NuGet Package Manager:

```powershell
Install-Package Huefy.Sdk
```

## Requirements

- .NET 10

## Quick Start

```csharp
using Huefy.Sdk;
using Huefy.Sdk.Models;

using var client = new HuefyEmailClient(new HuefyConfig
{
    ApiKey = Environment.GetEnvironmentVariable("HUEFY_API_KEY")!,
});

var response = await client.SendEmailAsync(new SendEmailRequest
{
    TemplateKey = "welcome-email",
    Recipient = new SendEmailRecipient
    {
        Email = "alice@example.com",
        Type = "cc",
        Data = new Dictionary<string, object?> { ["locale"] = "en" },
    },
    Data = new Dictionary<string, object?>
    {
        ["firstName"] = "Alice",
        ["trialDays"] = 14,
    },
});

Console.WriteLine($"Email ID: {response.Data.EmailId}");
```

## ASP.NET Core / Dependency Injection

```csharp
// Program.cs
using Huefy.Sdk;

builder.Services.AddSingleton(new HuefyEmailClient(new HuefyConfig
{
    ApiKey = builder.Configuration["Huefy:ApiKey"]!,
}));
```

This registers a singleton `HuefyEmailClient` instance for application-wide reuse.

```csharp
// MyService.cs
using Huefy.Sdk;
using Huefy.Sdk.Models;

public class MyService(HuefyEmailClient huefy)
{
    public async Task SendWelcomeAsync(string email)
    {
        await huefy.SendEmailAsync(new SendEmailRequest
        {
            TemplateKey = "welcome-email",
            Recipient = email,
            Data = new Dictionary<string, object?>(),
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
using Huefy.Sdk.Models;

var bulk = await client.SendBulkEmailsAsync(new SendBulkEmailsRequest
{
    TemplateKey = "promo",
    Recipients =
    [
        new BulkRecipient { Email = "bob@example.com" },
        new BulkRecipient { Email = "carol@example.com" },
    ],
});

Console.WriteLine($"Sent: {bulk.Data.SuccessCount}, Failed: {bulk.Data.FailureCount}");
```

## Error Handling

```csharp
using Huefy.Sdk.Errors;

try
{
    var response = await client.SendEmailAsync(request);
    Console.WriteLine($"Delivered: {response.Data.EmailId}");
}
catch (HuefyException e) when (e.Code == ErrorCode.AuthenticationFailed)
{
    Console.Error.WriteLine("Invalid API key");
}
catch (HuefyException e) when (e.Code == ErrorCode.RateLimited)
{
    Console.Error.WriteLine($"Rate limited. Retry after {e.RetryAfter}ms");
}
catch (HuefyException e) when (e.Code == ErrorCode.CircuitBreakerOpen)
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
if (health.Data.Status != "healthy")
{
    logger.LogWarning("Huefy degraded: {Status}", health.Data.Status);
}
```

## Local Development

`HUEFY_MODE=local` resolves to `https://api.huefy.on/api/v1/sdk`. To bypass Caddy and hit the raw app port directly, override `BaseUrl` to `http://localhost:8080/api/v1/sdk`:

```csharp
var client = new HuefyEmailClient(new HuefyConfig
{
    ApiKey = "sdk_local_key",
    BaseUrl = "https://api.huefy.on/api/v1/sdk",
});
```

Or via `appsettings.Development.json`:

```json
{
  "Huefy": {
    "ApiKey": "sdk_local_key",
    "BaseUrl": "https://api.huefy.on/api/v1/sdk"
  }
}
```

## Developer Guide

Full documentation, advanced patterns, and provider configuration are in the [.NET Developer Guide](../../docs/spec/guides/dotnet.guide.md).

## License

MIT
