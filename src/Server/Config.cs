using System.Text.Json;
using Gommon;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Ryujinx.Systems.Update.Server.Helpers;
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

    extension(WebApplicationBuilder builder)
    {
        public void TryUseVersionPinning()
        {
            if (CommandLineState.GenerateVersionPinning)
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
                builder.Configuration.Sources.Add(new JsonConfigurationSource
                {
                    FileProvider = DiskProvider,
                    Optional = true,
                    ReloadOnChange = false,
                    Path = "versionPinning.json"
                });
        }

        public void TryUseVersionProvider()
        {
            if (CommandLineState.GenerateVersionProvider)
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
}