using Huefy.Sdk.Errors;
using Huefy.Sdk.Security;
using Xunit;

namespace Huefy.Sdk.Tests;

public class SecurityTests
{
    // --- HMAC-SHA256 Tests ---

    [Fact]
    public void ComputeHmacSha256_ShouldReturnConsistentHash()
    {
        var signature1 = SecurityUtils.ComputeHmacSha256("payload", "secret");
        var signature2 = SecurityUtils.ComputeHmacSha256("payload", "secret");

        Assert.Equal(signature1, signature2);
        Assert.Equal(64, signature1.Length); // SHA-256 hex = 64 chars
    }

    [Fact]
    public void ComputeHmacSha256_ShouldDifferForDifferentPayloads()
    {
        var sig1 = SecurityUtils.ComputeHmacSha256("payload1", "secret");
        var sig2 = SecurityUtils.ComputeHmacSha256("payload2", "secret");

        Assert.NotEqual(sig1, sig2);
    }

    [Fact]
    public void ComputeHmacSha256_ShouldDifferForDifferentSecrets()
    {
        var sig1 = SecurityUtils.ComputeHmacSha256("payload", "secret1");
        var sig2 = SecurityUtils.ComputeHmacSha256("payload", "secret2");

        Assert.NotEqual(sig1, sig2);
    }

    [Fact]
    public void ComputeHmacSha256_ShouldThrowOnEmptyPayload()
    {
        Assert.Throws<ArgumentException>(() =>
            SecurityUtils.ComputeHmacSha256("", "secret"));
    }

    [Fact]
    public void ComputeHmacSha256_ShouldThrowOnEmptySecret()
    {
        Assert.Throws<ArgumentException>(() =>
            SecurityUtils.ComputeHmacSha256("payload", ""));
    }

    // --- Verification Tests ---

    [Fact]
    public void VerifyHmacSha256_ShouldReturnTrue_ForValidSignature()
    {
        var signature = SecurityUtils.ComputeHmacSha256("test-payload", "test-secret");

        Assert.True(SecurityUtils.VerifyHmacSha256("test-payload", "test-secret", signature));
    }

    [Fact]
    public void VerifyHmacSha256_ShouldReturnFalse_ForInvalidSignature()
    {
        Assert.False(SecurityUtils.VerifyHmacSha256("payload", "secret", "invalid-signature"));
    }

    [Fact]
    public void VerifyHmacSha256_ShouldReturnFalse_ForTamperedPayload()
    {
        var signature = SecurityUtils.ComputeHmacSha256("original", "secret");

        Assert.False(SecurityUtils.VerifyHmacSha256("tampered", "secret", signature));
    }

    // --- Signing String Tests ---

    [Fact]
    public void BuildSigningString_ShouldFormatCorrectly()
    {
        var result = SecurityUtils.BuildSigningString("POST", "/api/v1/test", 1700000000000, "{\"key\":\"value\"}");

        Assert.StartsWith("POST\n/api/v1/test\n1700000000000\n", result);
        Assert.Contains("\n", result);
    }

    [Fact]
    public void BuildSigningString_ShouldUppercaseMethod()
    {
        var result = SecurityUtils.BuildSigningString("get", "/path", 0, "");

        Assert.StartsWith("GET\n", result);
    }

    // --- PII Detection Tests ---

    [Fact]
    public void ContainsPii_ShouldDetectEmail()
    {
        Assert.True(SecurityUtils.ContainsPii("Contact user@example.com for info"));
    }

    [Fact]
    public void ContainsPii_ShouldDetectSsn()
    {
        Assert.True(SecurityUtils.ContainsPii("SSN: 123-45-6789"));
    }

    [Fact]
    public void ContainsPii_ShouldDetectCreditCard()
    {
        Assert.True(SecurityUtils.ContainsPii("Card: 4111-1111-1111-1111"));
    }

    [Fact]
    public void ContainsPii_ShouldDetectPhoneNumber()
    {
        Assert.True(SecurityUtils.ContainsPii("Call +1 (555) 123-4567"));
    }

    [Fact]
    public void ContainsPii_ShouldReturnFalse_ForCleanInput()
    {
        Assert.False(SecurityUtils.ContainsPii("This is a clean string with no PII"));
    }

    [Fact]
    public void ContainsPii_ShouldReturnFalse_ForNullOrEmpty()
    {
        Assert.False(SecurityUtils.ContainsPii(""));
        Assert.False(SecurityUtils.ContainsPii(null!));
    }

    // --- API Key Helpers ---

    [Fact]
    public void MaskApiKey_ShouldShowFirstAndLastFourChars()
    {
        var masked = SecurityUtils.MaskApiKey("sk_live_1234567890abcdef");

        Assert.StartsWith("sk_l", masked);
        Assert.EndsWith("cdef", masked);
        Assert.Contains("...", masked);
    }

    [Fact]
    public void MaskApiKey_ShouldReturnStars_ForShortKey()
    {
        Assert.Equal("***", SecurityUtils.MaskApiKey("short"));
    }

    [Fact]
    public void MaskApiKey_ShouldReturnStars_ForEmptyOrNull()
    {
        Assert.Equal("***", SecurityUtils.MaskApiKey(""));
        Assert.Equal("***", SecurityUtils.MaskApiKey(null!));
    }

    [Theory]
    [InlineData("valid_api_key_1234567890", true)]
    [InlineData("short", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void IsValidApiKeyFormat_ShouldValidateCorrectly(string key, bool expected)
    {
        Assert.Equal(expected, SecurityUtils.IsValidApiKeyFormat(key));
    }

    // --- Error Sanitizer Tests ---

    [Fact]
    public void Sanitizer_ShouldRedactEmail()
    {
        var result = ErrorSanitizer.Sanitize("Error for user@example.com");
        Assert.Contains("[REDACTED:EMAIL]", result);
        Assert.DoesNotContain("user@example.com", result);
    }

    [Fact]
    public void Sanitizer_ShouldRedactBearerToken()
    {
        var result = ErrorSanitizer.Sanitize("Authorization: Bearer eyJhbGciOiJIUzI1NiJ9");
        Assert.Contains("[REDACTED:BEARER_TOKEN]", result);
    }

    [Fact]
    public void Sanitizer_ShouldRedactIpAddress()
    {
        var result = ErrorSanitizer.Sanitize("Connection from 192.168.1.100 refused");
        Assert.Contains("[REDACTED:IP_ADDRESS]", result);
        Assert.DoesNotContain("192.168.1.100", result);
    }

    [Fact]
    public void Sanitizer_ShouldReturnOriginal_WhenNoSensitiveData()
    {
        const string clean = "This is a clean error message";
        Assert.Equal(clean, ErrorSanitizer.Sanitize(clean));
    }

    [Fact]
    public void Sanitizer_ContainsSensitiveData_ShouldDetect()
    {
        Assert.True(ErrorSanitizer.ContainsSensitiveData("user@example.com"));
        Assert.False(ErrorSanitizer.ContainsSensitiveData("clean string"));
    }

    [Fact]
    public void Sanitizer_ShouldHandleEmptyOrNull()
    {
        Assert.Equal("", ErrorSanitizer.Sanitize(""));
        Assert.Null(ErrorSanitizer.Sanitize(null!));
    }
}
