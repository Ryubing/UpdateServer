using System.Text.Json.Serialization;

namespace Ryujinx.Systems.Update.Server.Services.Forgejo;

public class Release
{
    [JsonPropertyName("assets")] public ICollection<Attachment>? Assets { get; set; }
    [JsonPropertyName("body")] public string? Body { get; set; }
    [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
    [JsonPropertyName("html_url")] public string? HtmlUrl { get; set; }
    [JsonPropertyName("id")] public long? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("prerelease")] public bool? PreRelease { get; set; }
    [JsonPropertyName("published_at")] public DateTimeOffset? PublishedAt { get; set; }
    [JsonPropertyName("tag_name")] public string? TagName { get; set; }
    [JsonPropertyName("tarball_url")] public string? TarballUrl { get; set; }
    [JsonPropertyName("target_commitish")] public string? TargetCommitish { get; set; }
    [JsonPropertyName("upload_url")] public string? UploadUrl { get; set; }
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("zipball_url")] public string? ZipballUrl { get; set; }
}

public class Attachment
{
    [JsonPropertyName("browser_download_url")] public string? DownloadUrl { get; set; }
    [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
    [JsonPropertyName("download_count")] public long? DownloadCount { get; set; }
    [JsonPropertyName("id")] public long? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("size")] public long? Size { get; set; }
    [JsonPropertyName("uuid")] public string? Uuid { get; set; }
}

[JsonSerializable(typeof(IEnumerable<Release>))]
public partial class ServerJsonSerializerContexts : JsonSerializerContext;