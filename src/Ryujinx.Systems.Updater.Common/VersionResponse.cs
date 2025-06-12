using System.Text.Json.Serialization;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Ryujinx.Systems.Updater.Common;

public class VersionResponse
{
    [JsonPropertyName("tag")]
    public required string Version { get; set; }
    
    [JsonPropertyName("download_url")]
    public required string ArtifactUrl { get; set; }
    
    [JsonPropertyName("web_url")]
    public required string ReleaseUrl { get; set; }
}

public class DownloadLinks
{
    [JsonPropertyName("windows")] public SupportedPlatform Windows { get; } = new();
    [JsonPropertyName("linux")] public SupportedPlatform Linux { get; } = new();
    [JsonPropertyName("linux_appimage")] public SupportedPlatform LinuxAppImage { get; } = new();
    // ReSharper disable once InconsistentNaming
    [JsonPropertyName("macOS")] public string MacOS { get; set; }

    public class SupportedPlatform
    {
        [JsonPropertyName("x64")] public string X64 { get; set; }
        [JsonPropertyName("arm64")] public string Arm64 { get; set; }
    }
}