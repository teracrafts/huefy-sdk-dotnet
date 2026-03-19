using System.Text.Json.Serialization;

namespace Huefy.Sdk.Models;

/// <summary>
/// Delivery status for a single recipient.
/// </summary>
public record RecipientStatus
{
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("sentAt")]
    public string? SentAt { get; init; }
}

/// <summary>
/// Data payload from the send email response.
/// </summary>
public record SendEmailResponseData
{
    [JsonPropertyName("emailId")]
    public string EmailId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("recipients")]
    public List<RecipientStatus> Recipients { get; init; } = [];

    [JsonPropertyName("scheduledAt")]
    public string? ScheduledAt { get; init; }

    [JsonPropertyName("sentAt")]
    public string? SentAt { get; init; }
}

/// <summary>
/// Response from the send email endpoint.
/// </summary>
public record SendEmailResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public SendEmailResponseData Data { get; init; } = new();

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; init; } = string.Empty;
}
