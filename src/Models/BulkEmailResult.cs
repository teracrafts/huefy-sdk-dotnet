using System.Text.Json.Serialization;

namespace Huefy.Sdk.Models;

/// <summary>
/// Error details for a single email in a bulk operation.
/// </summary>
public record BulkEmailError
{
    /// <summary>Error message describing what went wrong.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>Error code string.</summary>
    [JsonPropertyName("code")]
    public required string Code { get; init; }
}

/// <summary>
/// Result of sending a single email in a bulk operation.
/// </summary>
public record BulkEmailResult
{
    /// <summary>The recipient email address.</summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>Whether this individual email was sent successfully.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>The response if the email was sent successfully.</summary>
    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SendEmailResponse? Result { get; init; }

    /// <summary>The error if the email failed to send.</summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BulkEmailError? Error { get; init; }
}
