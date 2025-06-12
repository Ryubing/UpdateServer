using System.Text.Json.Serialization;

namespace RyujinxUpdate.Model;

[JsonSerializable(typeof(VersionCacheEntry))]
[JsonSerializable(typeof(VersionResponse))]
public partial class JsonSerializerContexts : JsonSerializerContext;