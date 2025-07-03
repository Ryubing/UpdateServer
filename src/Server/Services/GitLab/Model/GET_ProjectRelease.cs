﻿using System.Text.Json.Serialization;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Ryujinx.Systems.Update.Common;

public class GitLabReleaseJsonResponse
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; set; }
    [JsonPropertyName("tag_name")] public string TagName { get; set; }
    [JsonPropertyName("author")] public GitLabUserJsonResponse Author { get; set; }
    [JsonPropertyName("_links")] public WebLinks Links { get; set; }
    
    [JsonPropertyName("assets")] public GitLabReleaseAssetsJsonResponse Assets { get; set; }
    
    public class GitLabReleaseAssetsJsonResponse
    {
        [JsonPropertyName("links")]
        public AssetLink[] Links { get; set; }
    }
    
    public class AssetLink
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("name")] public string AssetName { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
    }


    public class GitLabUserJsonResponse
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("avatar_url")] public string AvatarUrl { get; set; }
    }
    
    public class WebLinks
    {
        [JsonPropertyName("self")] public string Self { get; set; }
    }
}

[JsonSerializable(typeof(IEnumerable<GitLabReleaseJsonResponse>))]
public partial class JsonSerializerContexts : JsonSerializerContext;