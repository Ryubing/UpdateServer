using System.Text.Json.Serialization;

namespace Ryujinx.Systems.Update.Common;

public class CacheSourceMapping
{
    [JsonPropertyName("stable")]
    public required VersionCacheSource Stable { get; set; }

    [JsonPropertyName("canary")]
    public required VersionCacheSource? Canary { get; set; }
}