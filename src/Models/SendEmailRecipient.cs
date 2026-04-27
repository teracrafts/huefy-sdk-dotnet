using System.Text.Json.Serialization;

namespace Huefy.Sdk.Models;

/// <summary>
/// Expanded recipient object supported by the send email API.
/// </summary>
public record SendEmailRecipient
{
    /// <summary>The recipient email address.</summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>Optional recipient type such as to, cc, or bcc.</summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }

    /// <summary>Optional recipient-scoped template data.</summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object?>? Data { get; init; }
}
