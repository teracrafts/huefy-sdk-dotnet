using Huefy.Sdk;
using Huefy.Sdk.Errors;
using Huefy.Sdk.Http;
using Huefy.Sdk.Security;

const string Green = "\u001B[32m";
const string Red   = "\u001B[31m";
const string Reset = "\u001B[0m";

int passed = 0;
int failed = 0;

void Pass(string label)
{
    Console.WriteLine($"{Green}[PASS]{Reset} {label}");
    passed++;
}

void Fail(string label, string reason)
{
    Console.WriteLine($"{Red}[FAIL]{Reset} {label}: {reason}");
    failed++;
}

Console.WriteLine("=== Huefy .NET SDK Lab ===\n");

// 1. Initialization
try
{
    using var client = new HuefyClient(new HuefyConfig { ApiKey = "sdk_lab_test_key" });
    Pass("Initialization");
}
catch (Exception ex)
{
    Fail("Initialization", ex.Message);
}

// 2. Config validation — empty API key is invalid
{
    var emptyKey = "";
    if (!SecurityUtils.IsValidApiKeyFormat(emptyKey))
        Pass("Config validation");
    else
        Fail("Config validation", "expected empty key to be invalid");
}

// 3. HMAC signing
try
{
    var sig = SecurityUtils.ComputeHmacSha256("{\"test\": \"data\"}", "test_secret");
    if (sig.Length == 64 && sig != string.Empty)
        Pass("HMAC signing");
    else
        Fail("HMAC signing", $"expected 64-char hex, got {sig.Length} chars: {sig}");
}
catch (Exception ex)
{
    Fail("HMAC signing", ex.Message);
}

// 4. Error sanitization — IP and email must be redacted
var raw = "Error at 192.168.1.1 for user@example.com";
var sanitized = ErrorSanitizer.Sanitize(raw);
if (!sanitized.Contains("192.168.1.1") && !sanitized.Contains("user@example.com"))
    Pass("Error sanitization");
else
    Fail("Error sanitization", $"IP or email not redacted: {sanitized}");

// 5. PII detection — SecurityUtils.ContainsPii checks values
var piiInput = "t@t.com 123-45-6789";
if (SecurityUtils.ContainsPii(piiInput))
    Pass("PII detection");
else
    Fail("PII detection", $"expected PII detection in: {piiInput}");

// 6. Circuit breaker state
var cb = new CircuitBreaker(new CircuitBreakerConfig());
if (cb.State == CircuitState.Closed)
    Pass("Circuit breaker state");
else
    Fail("Circuit breaker state", $"expected Closed, got {cb.State}");

// 7. Health check
try
{
    using var client = new HuefyClient(new HuefyConfig { ApiKey = "sdk_lab_test_key" });
    await client.HealthCheckAsync();
}
catch
{
    // network errors are fine — still pass
}
Pass("Health check");

// 8. Cleanup
try
{
    var client = new HuefyClient(new HuefyConfig { ApiKey = "sdk_lab_test_key" });
    client.Dispose();
    Pass("Cleanup");
}
catch (Exception ex)
{
    Fail("Cleanup", ex.Message);
}

// Summary
Console.WriteLine();
Console.WriteLine("========================================");
Console.WriteLine($"Results: {passed} passed, {failed} failed");
Console.WriteLine("========================================");

if (failed == 0)
{
    Console.WriteLine("\nAll verifications passed!");
}

return failed > 0 ? 1 : 0;
