namespace Ryujinx.Systems.Update.Client;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

public delegate void UpdateClientLogCallback(string format, object[] formatArgs, string caller);

public class UpdateClientConfig
{
    internal UpdateClientConfig() {}
    
    public required UpdateClientLogCallback Logger;
    public required string ServerEndpoint { get; init; }
    public string? AdminAccessToken { get; init; }

    public bool CanUseAdminEndpoints => AdminAccessToken != null;

    #region Factory methods

    public static UpdateClientConfig StandardUnauthorized(string serverEndpoint = "https://update.ryujinx.app")
        => new Builder()
            .WithServerEndpoint(serverEndpoint)
            .WithStdOutLogger()
            .Build();

    public static UpdateClientConfig StandardAuthorized(string accessToken,
        string serverEndpoint = "https://update.ryujinx.app")
        => new Builder()
            .WithServerEndpoint(serverEndpoint)
            .WithAccessToken(accessToken)
            .WithStdOutLogger()
            .Build();

    #endregion

    public class Builder
    {
        // default is no logging
        public UpdateClientLogCallback Logger { get; private set; } = (_, _, _) => { };
        public string ServerEndpoint { get; private set; } = "https://update.ryujinx.app";
        public string? AdminAccessToken { get; private set; }

        public Builder WithStdOutLogger()
        {
            Logger = (format, args, _) =>
                Console.WriteLine(args.Length == 0 ? format : string.Format(format, args));
            return this;
        }
        
        public Builder WithLogger(UpdateClientLogCallback callback)
        {
            Logger = callback;
            return this;
        }
        
        public Builder WithServerEndpoint(string serverEndpoint)
        {
            ServerEndpoint = serverEndpoint ?? throw new NullReferenceException("Cannot use null as a server endpoint.");
            return this;
        }

        public Builder WithAccessToken(string accessToken)
        {
            AdminAccessToken = accessToken;
            return this;
        }

        public UpdateClientConfig Build()
            => new()
            {
                ServerEndpoint = ServerEndpoint,
                Logger = Logger,
                AdminAccessToken = AdminAccessToken
            };
    }
}