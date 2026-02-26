using System.Text.Json.Serialization;

namespace Huefy.Sdk.Models;

/// <summary>
/// Response from the send email endpoint.
/// </summary>
public record SendEmailResponse
{
    /// <summary>Whether the email was sent successfully.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>A human-readable message from the server.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>The unique identifier for the sent message.</summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; init; }

    /// <summary>The provider that was used to deliver the email.</summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; init; }
}
