using System.Text.Json.Serialization;
using RyujinxUpdate.Services.GitLab;

namespace RyujinxUpdate.Model;

public class VersionResponse
{
    [JsonPropertyName("tag")]
    public string Version { get; set; }
    
    [JsonPropertyName("download_url")]
    public string ArtifactUrl { get; set; }
}

public class VersionDumpResponse
{
    [JsonPropertyName("tag")]
    public string Version { get; set; }

    [JsonPropertyName("downloads")] public DownloadLinks Links { get; set; } = new();

    public static VersionDumpResponse FromVersionCache(VersionCache.Entry vcacheEntry) =>
        new()
        {
            Version = vcacheEntry.Tag,
            Links =
            {
                Windows =
                {
                    X64 = vcacheEntry.Downloads.WindowsX64,
                    Arm64 = string.Empty
                },
                Linux =
                {
                    X64 = vcacheEntry.Downloads.LinuxX64,
                    Arm64 = vcacheEntry.Downloads.LinuxArm64
                },
                LinuxAppImage =
                {
                    X64 = vcacheEntry.Downloads.LinuxAppImageX64,
                    Arm64 = vcacheEntry.Downloads.LinuxAppImageArm64
                },
                MacOS = vcacheEntry.Downloads.MacOsUniversal
            }
        };
}

public class DownloadLinks
{
    [JsonPropertyName("windows")] public ArchitectureTuple Windows { get; } = new();
    [JsonPropertyName("linux")] public ArchitectureTuple Linux { get; } = new();
    [JsonPropertyName("linux_appimage")] public ArchitectureTuple LinuxAppImage { get; } = new();
    [JsonPropertyName("macOS")] public string MacOS { get; set; }

    public class ArchitectureTuple
    {
        [JsonPropertyName("x64")] public string X64 { get; set; }
        [JsonPropertyName("arm64")] public string Arm64 { get; set; }
    }
}