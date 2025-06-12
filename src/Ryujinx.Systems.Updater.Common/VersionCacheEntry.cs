using System.Text.Json.Serialization;

namespace RyujinxUpdate.Model;

public class VersionCacheEntry
{
    [JsonPropertyName("tag")] public required string Tag { get; set; }
    [JsonPropertyName("web_url")] public required string ReleaseUrl { get; set; }
    [JsonPropertyName("downloads")] public DownloadLinks Downloads { get; } = new();
}