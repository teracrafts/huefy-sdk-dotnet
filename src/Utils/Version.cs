namespace Teracrafts.Huefy.Sdk.Utils;

/// <summary>
/// SDK version information.
/// </summary>
public static class SdkVersion
{
    /// <summary>
    /// The current SDK version string.
    /// </summary>
    public const string Current = "1.0.0";

    /// <summary>
    /// The SDK user-agent header value.
    /// </summary>
    public static string UserAgent => $"huefy-dotnet/{Current}";
}
