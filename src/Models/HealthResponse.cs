using System.Text.Json.Serialization;

namespace Teracrafts.Huefy.Sdk.Models;

/// <summary>
/// Data payload from the health check response.
/// </summary>
public record HealthResponseData
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string? Version { get; init; }
}

/// <summary>
/// Response from the health check endpoint.
/// </summary>
public record EmailHealthResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public HealthResponseData Data { get; init; } = new();

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; init; } = string.Empty;
}
