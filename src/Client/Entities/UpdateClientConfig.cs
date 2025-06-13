namespace Ryujinx.Systems.Update.Client;

public class UpdateClientConfig
{
    public static UpdateClientConfig Unauthorized(string serverEndpoint = "https://update.ryujinx.app") => new()
    {
        ServerEndpoint = serverEndpoint,
        AdminAccessToken = null,
        Logger = (format, args) => Console.WriteLine(args.Length == 0 ? format : string.Format(format, args))
    };
    
    public static UpdateClientConfig WithAuthorization(string accessToken, string serverEndpoint = "https://update.ryujinx.app") => new()
    {
        ServerEndpoint = serverEndpoint,
        AdminAccessToken = accessToken,
        Logger = (format, args) => Console.WriteLine(args.Length == 0 ? format : string.Format(format, args))
    };
    
    public required Action<string, object[]> Logger;
    public required string ServerEndpoint { get; set; }
    public required string? AdminAccessToken { get; init; }

    public bool CanUseAdminEndpoints => AdminAccessToken != null;
}