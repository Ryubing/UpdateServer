using CommandLine;

namespace Ryujinx.Systems.Update.Server.Helpers;

public class CommandLineState
{
    public static bool HttpLogging => Instance.UseHttpLogging;
    public static bool Swagger => Instance.UseSwagger;
    public static int? Port => Instance.ListenPort;
    public static bool GenerateVersionPinning => Instance.GenerateVersionPinningConfiguration;
    public static bool GenerateVersionProvider => Instance.GenerateVersionProviderConfiguration;

    public static bool Init(string[] args)
    {
        ParserResult<CommandLineState> parserResult = Parser.Default.ParseArguments<CommandLineState>(args);

        if (parserResult is not Parsed<CommandLineState> parsedCls)
            return false;

        Instance = parsedCls.Value;

        return true;
    }

    private static CommandLineState Instance { get; set; } = null!;

    [Option('h',"http-logging", Required = false, Default = 
#if DEBUG
            true
#else
            false
#endif
        , HelpText = "Register ASP.NET HTTP logging."
        )]
    public bool UseHttpLogging { get; set; }

    [Option('s',"swagger-ui", Required = false, Default = 
#if DEBUG
            true
#else
            false
#endif
        , HelpText = "Enable Swagger UI at the <ServerUrl>/swagger endpoint. /docs, /info, and /help redirect there as well with this enabled."
    )]
    public bool UseSwagger { get; set; } =
#if DEBUG
        true
#else
        false
#endif
        ;

    [Option('p', "port", Required = false, Default = null, HelpText = "Specifies the port to listen on.")]
    public int? ListenPort { get; set; }

    [Option("gen-version-pinning", Required = false, Default = false, HelpText = "Generates a template configuration for the version pinning system.")]
    public bool GenerateVersionPinningConfiguration { get; set; }

    [Option("gen-version-provider", Required = false, Default = false, HelpText = "Generates a template configuration for the version provider system.")]
    public bool GenerateVersionProviderConfiguration { get; set; }
}