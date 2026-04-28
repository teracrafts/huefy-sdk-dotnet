using System.Text.Json.Serialization;

namespace Teracrafts.Huefy.Sdk.Models;

/// <summary>
/// Supported email providers for the Huefy API.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmailProvider
{
    /// <summary>Amazon Simple Email Service.</summary>
    [JsonPropertyName("ses")]
    Ses,

    /// <summary>SendGrid email provider.</summary>
    [JsonPropertyName("sendgrid")]
    Sendgrid,

    /// <summary>Mailgun email provider.</summary>
    [JsonPropertyName("mailgun")]
    Mailgun,

    /// <summary>Mailchimp email provider.</summary>
    [JsonPropertyName("mailchimp")]
    Mailchimp
}
