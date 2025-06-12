using System.Text.Json.Serialization;

namespace Ryujinx.Systems.Updater.Common;

[JsonSerializable(typeof(VersionCacheEntry))]
[JsonSerializable(typeof(VersionResponse))]
public partial class JsonSerializerContexts : JsonSerializerContext;