using System.Text.Json.Serialization;

namespace Ryujinx.Systems.Update.Common;

public class VersionCacheEntry
{
    [JsonPropertyName("tag")] public required string Tag { get; set; }
    [JsonPropertyName("web_url")] public required string ReleaseUrl { get; set; }
    [JsonPropertyName("downloads")] public required DownloadLinks Downloads { get; set; }

    public string GetUrlFor(SupportedPlatform platform, SupportedArchitecture architecture)
    {
        if (platform is SupportedPlatform.Mac)
            return Downloads.MacOS;

        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        var p = platform switch
        {
            SupportedPlatform.Windows => Downloads.Windows,
            SupportedPlatform.Linux => Downloads.Linux,
            SupportedPlatform.LinuxAppImage => Downloads.LinuxAppImage,
            _ => throw new ArgumentOutOfRangeException(nameof(platform))
        };

        return architecture switch
        {
            SupportedArchitecture.Arm64 => p.Arm64,
            SupportedArchitecture.Amd64 => p.X64,
            _ => throw new ArgumentOutOfRangeException(nameof(architecture))
        };
    }

    public static (SupportedPlatform Platform, SupportedArchitecture Architecture)
        GetVersionTupleForUserAgent(string ua)
    {
        if (ua.Contains("Mac", StringComparison.OrdinalIgnoreCase))
            return (SupportedPlatform.Mac, SupportedArchitecture.Arm64);

        return (
            ua.Contains("Linux", StringComparison.OrdinalIgnoreCase)
                ? SupportedPlatform.Linux
                : SupportedPlatform.Windows,
            ua.Contains("x64", StringComparison.OrdinalIgnoreCase)
                ? SupportedArchitecture.Amd64
                : SupportedArchitecture.Arm64
        );
    }
}