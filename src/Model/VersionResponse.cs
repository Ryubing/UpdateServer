using System.Text.Json.Serialization;

namespace RyujinxUpdate.Model;

public class VersionResponse
{
    [JsonPropertyName("tag")]
    public string Version { get; set; }
    
    [JsonPropertyName("download_url")]
    public string ArtifactUrl { get; set; }
}