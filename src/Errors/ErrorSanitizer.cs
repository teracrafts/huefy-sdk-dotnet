using System.Text.RegularExpressions;

namespace Huefy.Sdk.Errors;

/// <summary>
/// Sanitizes error messages and payloads by redacting sensitive information.
/// </summary>
public static partial class ErrorSanitizer
{
    private static readonly (Regex Pattern, string Label)[] SanitizationRules =
    [
        (EmailRegex(), "EMAIL"),
        (ApiKeyRegex(), "API_KEY"),
        (BearerTokenRegex(), "BEARER_TOKEN"),
        (CreditCardRegex(), "CREDIT_CARD"),
        (SsnRegex(), "SSN"),
        (PhoneRegex(), "PHONE"),
        (IpAddressRegex(), "IP_ADDRESS"),
        (JwtRegex(), "JWT"),
    ];

    /// <summary>
    /// Sanitizes sensitive data from the input string, replacing matches with [REDACTED:LABEL].
    /// </summary>
    public static string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = input;
        foreach (var (pattern, label) in SanitizationRules)
        {
            result = pattern.Replace(result, $"[REDACTED:{label}]");
        }

        return result;
    }

    /// <summary>
    /// Returns true if the input contains any detectable sensitive data.
    /// </summary>
    public static bool ContainsSensitiveData(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        foreach (var (pattern, _) in SanitizationRules)
        {
            if (pattern.IsMatch(input))
                return true;
        }

        return false;
    }

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(?:api[_-]?key|apikey|x-api-key)[""']?\s*[:=]\s*[""']?[\w\-]{16,}[""']?", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ApiKeyRegex();

    [GeneratedRegex(@"Bearer\s+[\w\-\.]+", RegexOptions.Compiled)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex(@"\b\d{4}[\s\-]?\d{4}[\s\-]?\d{4}[\s\-]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex CreditCardRegex();

    [GeneratedRegex(@"\b\d{3}[\s\-]?\d{2}[\s\-]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex SsnRegex();

    [GeneratedRegex(@"\+?\d{1,3}[\s\-]?\(?\d{2,3}\)?[\s\-]?\d{3,4}[\s\-]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled)]
    private static partial Regex IpAddressRegex();

    [GeneratedRegex(@"eyJ[a-zA-Z0-9_-]+\.eyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+", RegexOptions.Compiled)]
    private static partial Regex JwtRegex();
}
