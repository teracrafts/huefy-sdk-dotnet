using System.Text.Json.Serialization;

namespace Huefy.Sdk.Models;

/// <summary>
/// Request to send a single email via the Huefy API.
/// </summary>
public record SendEmailRequest
{
    /// <summary>The template key identifying the email template (1-100 characters).</summary>
    [JsonPropertyName("templateKey")]
    public required string TemplateKey { get; init; }

    /// <summary>Template data variables to merge into the email.</summary>
    [JsonPropertyName("data")]
    public required Dictionary<string, object?> Data { get; init; }

    /// <summary>
    /// The recipient, either as an email string or a <see cref="SendEmailRecipient"/> object.
    /// </summary>
    [JsonPropertyName("recipient")]
    public required object Recipient { get; init; }

    /// <summary>The email provider to use. Defaults to SES if not specified.</summary>
    [JsonPropertyName("providerType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EmailProvider? ProviderType { get; init; }
}
