using System.Text.Json.Serialization;

namespace Ryujinx.Systems.Update.Common;

public class VersionCacheSource
{
    internal static VersionCacheSource Empty => new()
    {
        Id = -1,
        Owner = string.Empty,
        Project = string.Empty
    };

    [JsonPropertyName("id")] public required long Id { get; set; }
    [JsonPropertyName("owner")] public required string Owner { get; set; }
    [JsonPropertyName("project")] public required string Project { get; set; }
}