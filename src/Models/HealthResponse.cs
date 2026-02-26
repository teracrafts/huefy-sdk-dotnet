using System.Text.Json.Serialization;

namespace Huefy.Sdk.Models;

/// <summary>
/// Response from the health check endpoint.
/// </summary>
public record EmailHealthResponse
{
    /// <summary>The status of the API (e.g., "ok").</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>Server timestamp.</summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    /// <summary>The API version string.</summary>
    [JsonPropertyName("version")]
    public string? Version { get; init; }
}
