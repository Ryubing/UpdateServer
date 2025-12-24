using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Gommon;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Ryujinx.Systems.Update.Server.Services;

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

    public static void TryUseVersionProvider(this WebApplicationBuilder builder, string[] args)
    {
        if (args.Any(x => x.EqualsIgnoreCase("--gen-version-provider")))
        {
            if (!VersionProvider.Path.ExistsAsFile)
                VersionProvider.Path.WriteAllText( 
                    JsonSerializer.Serialize(new VersionProvider
                    {
                        Stable = new()
                        {
                            Format = "1.{MAJOR}.{BUILD}",
                            Major = 3,
                            Build = 0
                        },
                        Canary = new()
                        {
                            Format = "1.{MAJOR}.{BUILD}",
                            Major = 3,
                            Build = 0
                        }
                    }, JSCtx.ReadableDefault.VersionProvider));
        }

        if (VersionProvider.Path.ExistsAsFile)
            builder.Services.AddSingleton<VersionProviderService>();
    }
}