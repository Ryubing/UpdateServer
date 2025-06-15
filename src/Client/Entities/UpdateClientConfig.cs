namespace Ryujinx.Systems.Update.Client;

public class UpdateClientConfig
{
    public static UpdateClientConfig Unauthorized(string serverEndpoint = "https://update.ryujinx.app") => new()
    {
        ServerEndpoint = serverEndpoint,
        AdminAccessToken = null,
        Logger = (arg, formatArgs) => 
            Console.WriteLine(formatArgs.Length == 0 ? arg.MessageFormat : string.Format(arg.MessageFormat, formatArgs))
    };
    
    public static UpdateClientConfig WithAuthorization(string accessToken, string serverEndpoint = "https://update.ryujinx.app") => new()
    {
        ServerEndpoint = serverEndpoint,
        AdminAccessToken = accessToken,
        Logger = (arg, formatArgs) 
            => Console.WriteLine(formatArgs.Length == 0 ? arg.MessageFormat : string.Format(arg.MessageFormat, formatArgs))
    };
    
    public required Action<(string MessageFormat, string Caller), object[]> Logger;
    public required string ServerEndpoint { get; set; }
    public required string? AdminAccessToken { get; init; }

    public bool CanUseAdminEndpoints => AdminAccessToken != null;
}