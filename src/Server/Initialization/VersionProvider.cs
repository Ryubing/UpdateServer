using System.Text.Json;
using System.Text.Json.Serialization;
using Gommon;

namespace Ryujinx.Systems.Update.Server;


[JsonSerializable(typeof(VersionProvider))]
internal partial class JSCtx : JsonSerializerContext
{
    public static JSCtx ReadableDefault { get; } = new(new JsonSerializerOptions
    {
        WriteIndented = true
    });
}

public class VersionProvider
{
    public Entry Stable { get; set; } = new();
    public Entry Canary { get; set; } = new();

    public void IncrementAndReset()
    {
        Stable.Major++;
        Stable.Build = -1;
        Canary.Major = Stable.Major;
        Canary.Build = 0;
        Save();
    }

    public void Save() 
        => File.WriteAllText("config/versionProvider.json", 
            JsonSerializer.Serialize(this, JSCtx.ReadableDefault.VersionProvider)
        );

    public static VersionProvider? Read() =>
        JsonSerializer.Deserialize(File.ReadAllText("config/versionProvider.json"),
            JSCtx.ReadableDefault.VersionProvider);

    public record Entry
    {
        public string Format { get; set; } = "1.{MAJOR}.{BUILD}";
        public ulong Major { get; set; }
        public long Build { get; set; }

        public Entry CopyIncrement() => this with { Build = Build + 1 };
        public Entry CopyIncrementMajor() => this with { Major = Major + 1 };

        public override string ToString() =>
            Format
                .ReplaceIgnoreCase("{MAJOR}", Major)
                .ReplaceIgnoreCase("{BUILD}", Build);
    }
}