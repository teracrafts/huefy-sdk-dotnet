using System.Text.Json.Serialization;

namespace Huefy.Sdk.Models;

/// <summary>
/// A single recipient in a bulk email send.
/// </summary>
public record BulkRecipient
{
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Data { get; init; }
}

/// <summary>
/// Request body for sending bulk emails.
/// </summary>
public record SendBulkEmailsRequest
{
    [JsonPropertyName("templateKey")]
    public required string TemplateKey { get; init; }

    [JsonPropertyName("recipients")]
    public required List<BulkRecipient> Recipients { get; init; }

    [JsonPropertyName("providerType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EmailProvider? Provider { get; init; }
}

/// <summary>
/// Data payload from the send-bulk response.
/// </summary>
public record SendBulkEmailsResponseData
{
    [JsonPropertyName("batchId")]
    public string BatchId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("templateKey")]
    public string TemplateKey { get; init; } = string.Empty;

    [JsonPropertyName("templateVersion")]
    public int TemplateVersion { get; init; }

    [JsonPropertyName("senderUsed")]
    public string SenderUsed { get; init; } = string.Empty;

    [JsonPropertyName("senderVerified")]
    public bool SenderVerified { get; init; }

    [JsonPropertyName("totalRecipients")]
    public int TotalRecipients { get; init; }

    [JsonPropertyName("processedCount")]
    public int ProcessedCount { get; init; }

    [JsonPropertyName("successCount")]
    public int SuccessCount { get; init; }

    [JsonPropertyName("failureCount")]
    public int FailureCount { get; init; }

    [JsonPropertyName("suppressedCount")]
    public int SuppressedCount { get; init; }

    [JsonPropertyName("startedAt")]
    public string StartedAt { get; init; } = string.Empty;

    [JsonPropertyName("completedAt")]
    public string? CompletedAt { get; init; }

    [JsonPropertyName("recipients")]
    public List<RecipientStatus> Recipients { get; init; } = [];

    [JsonPropertyName("errors")]
    public List<Dictionary<string, object?>> Errors { get; init; } = [];

    [JsonPropertyName("metadata")]
    public Dictionary<string, object?>? Metadata { get; init; }
}

/// <summary>
/// Response from the send-bulk endpoint.
/// </summary>
public record SendBulkEmailsResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public SendBulkEmailsResponseData Data { get; init; } = new();

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; init; } = string.Empty;
}
