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