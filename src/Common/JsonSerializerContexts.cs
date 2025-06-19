using System.Text.Json.Serialization;

namespace Ryujinx.Systems.Update.Common;

[JsonSerializable(typeof(VersionCacheEntry))]
[JsonSerializable(typeof(VersionResponse))]
public partial class JsonSerializerContexts : JsonSerializerContext;