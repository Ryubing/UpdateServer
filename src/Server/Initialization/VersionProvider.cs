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
    public static readonly FilePath Path = new("config/versionProvider.json", isDirectory: false);

    public Entry Stable { get; set; } = new();
    public Entry Canary { get; set; } = new();

    public void IncrementAndReset()
    {
        Stable = Stable.NextMajor();
        Canary = Canary.NextMajor();
        if (Stable.Major != Canary.Major) 
            Canary.Major = Stable.Major;
        
        Save();
    }

    public void Save() 
        => Path.WriteAllText(JsonSerializer.Serialize(this, JSCtx.ReadableDefault.VersionProvider));

    public static VersionProvider? Read() =>
        JsonSerializer.Deserialize(Path.ReadAllText(),
            JSCtx.ReadableDefault.VersionProvider);

    public record Entry
    {
        public string Format { get; set; } = "1.{MAJOR}.{BUILD}";
        public ulong Major { get; set; }
        public long Build { get; set; }

        public Entry NextBuild() => this with { Build = Build + 1 };
        public Entry NextMajor() => this with { Major = Major + 1, Build = 0 };

        public override string ToString() =>
            Format
                .ReplaceIgnoreCase("{MAJOR}", Major)
                .ReplaceIgnoreCase("{BUILD}", Build);
    }
}