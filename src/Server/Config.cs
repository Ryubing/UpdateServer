using System.Diagnostics.CodeAnalysis;
using Gommon;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;

namespace Ryujinx.Systems.Update.Server;

internal static class Config
{
    private static readonly IFileProvider DiskProvider;

    static Config()
    {
        if (!Directory.Exists("config"))
            Directory.CreateDirectory("config");


        DiskProvider = new PhysicalFileProvider(new FilePath(Environment.CurrentDirectory) / "config");
    }

    public static bool UseVersionPinning(string[] args,
        [MaybeNullWhen(false)] out JsonConfigurationSource jcs)
    {
        jcs = null;

        if (args.Any(x => x.EqualsIgnoreCase("--gen-version-pinning")))
        {
            if (!File.Exists("config/versionPinning.json"))
                File.WriteAllText("config/versionPinning.json",
                    """
                    {
                        "VersionPinning": {
                            "Stable": {
                                "osx": {
                                    "x64": "1.3.4"
                                }
                            }
                        }
                    }
                    """
                );
        }

        if (File.Exists("config/versionPinning.json")) 
            jcs = new()
            {
                FileProvider = DiskProvider,
                Optional = true,
                ReloadOnChange = false,
                Path = "versionPinning.json"
            };

        return jcs != null;
    }
}