using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Ryujinx.Systems.Update.Common;

public class VersionResponse
{
    [JsonPropertyName("tag")]
    public required string Version { get; set; }
    
    [JsonPropertyName("download_url")]
    public required string ArtifactUrl { get; set; }
    
    [JsonPropertyName("web_url")] public string ReleaseUrl  => string.Format(ReleaseUrlFormat, Version);
    
    [JsonPropertyName("web_url_format")]
    public required string ReleaseUrlFormat { get; set; }
}

public class DownloadLinks
{
    [JsonPropertyName("windows")] public required SupportedPlatform Windows { get; init; }
    [JsonPropertyName("linux")] public required SupportedPlatform Linux { get; init; }
    [JsonPropertyName("linux_appimage")] public required SupportedPlatform LinuxAppImage { get; init; }
    // ReSharper disable once InconsistentNaming
    [JsonPropertyName("macOS")] public required string MacOS { get; init; }

    public class SupportedPlatform
    {
        [JsonPropertyName("x64")] public required string X64 { get; init; }
        [JsonPropertyName("arm64")] public required string Arm64 { get; init; }
    }
}