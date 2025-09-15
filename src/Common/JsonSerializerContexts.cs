using System.Text.Json.Serialization;

namespace Ryujinx.Systems.Update.Common;

[JsonSerializable(typeof(VersionCacheEntry[]))]
[JsonSerializable(typeof(VersionResponse[]))]
[JsonSerializable(typeof(IEnumerable<VersionCacheEntry>))]
[JsonSerializable(typeof(IEnumerable<VersionResponse>))]
[JsonSerializable(typeof(Dictionary<string, VersionCacheSource>))]
public partial class JsonSerializerContexts : JsonSerializerContext;