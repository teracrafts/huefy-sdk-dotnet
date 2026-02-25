using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Huefy.Sdk.Security;

/// <summary>
/// Security utilities: HMAC-SHA256 request signing, PII detection, and key helpers.
/// </summary>
public static partial class SecurityUtils
{
    /// <summary>
    /// Computes an HMAC-SHA256 signature for the given payload using the provided secret.
    /// Returns the signature as a lowercase hexadecimal string.
    /// </summary>
    public static string ComputeHmacSha256(string payload, string secret)
    {
        ArgumentException.ThrowIfNullOrEmpty(payload);
        ArgumentException.ThrowIfNullOrEmpty(secret);

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        var hash = HMACSHA256.HashData(keyBytes, payloadBytes);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Verifies an HMAC-SHA256 signature using constant-time comparison.
    /// </summary>
    public static bool VerifyHmacSha256(string payload, string secret, string expectedSignature)
    {
        var computed = ComputeHmacSha256(payload, secret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(expectedSignature));
    }

    /// <summary>
    /// Builds the canonical signing string for an HTTP request.
    /// Format: METHOD\nPATH\nTIMESTAMP\nBODY_HASH
    /// </summary>
    public static string BuildSigningString(string method, string path, long timestamp, string body)
    {
        var bodyHash = ComputeSha256(body);
        return $"{method.ToUpperInvariant()}\n{path}\n{timestamp}\n{bodyHash}";
    }

    /// <summary>
    /// Computes SHA-256 hash of the input string, returned as lowercase hex.
    /// </summary>
    public static string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Detects whether the input contains personally identifiable information (PII).
    /// </summary>
    public static bool ContainsPii(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        return EmailPattern().IsMatch(input)
            || SsnPattern().IsMatch(input)
            || CreditCardPattern().IsMatch(input)
            || PhonePattern().IsMatch(input);
    }

    /// <summary>
    /// Masks an API key for safe logging, showing only the first 4 and last 4 characters.
    /// </summary>
    public static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return "***";

        if (apiKey.Length <= 8)
            return "***";

        return $"{apiKey[..4]}...{apiKey[^4..]}";
    }

    /// <summary>
    /// Validates that an API key meets minimum format requirements.
    /// </summary>
    public static bool IsValidApiKeyFormat(string apiKey) =>
        !string.IsNullOrWhiteSpace(apiKey) && apiKey.Length >= 16;

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\b\d{3}[\s\-]?\d{2}[\s\-]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex SsnPattern();

    [GeneratedRegex(@"\b\d{4}[\s\-]?\d{4}[\s\-]?\d{4}[\s\-]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex CreditCardPattern();

    [GeneratedRegex(@"\+?\d{1,3}[\s\-]?\(?\d{2,3}\)?[\s\-]?\d{3,4}[\s\-]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex PhonePattern();
}
