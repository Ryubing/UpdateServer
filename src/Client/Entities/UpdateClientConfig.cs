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
        => new BuilderApi()
            .WithServerEndpoint(serverEndpoint)
            .WithStdOutLogger()
            .Build();

    public static UpdateClientConfig StandardAuthorized(string accessToken,
        string serverEndpoint = "https://update.ryujinx.app")
        => new BuilderApi()
            .WithServerEndpoint(serverEndpoint)
            .WithAccessToken(accessToken)
            .WithStdOutLogger()
            .Build();

    #endregion

    public static BuilderApi Builder() => new();

    public class BuilderApi
    {
        // default is no logging
        public UpdateClientLogCallback Logger { get; private set; } = (_, _, _) => { };
        public string ServerEndpoint { get; private set; } = "https://update.ryujinx.app";
        public string? AdminAccessToken { get; private set; }

        public BuilderApi WithStdOutLogger()
        {
            Logger = (format, args, _) =>
                Console.WriteLine(args.Length == 0 ? format : string.Format(format, args));
            return this;
        }
        
        public BuilderApi WithLogger(UpdateClientLogCallback callback)
        {
            Logger = callback;
            return this;
        }
        
        public BuilderApi WithServerEndpoint(string serverEndpoint)
        {
            ServerEndpoint = serverEndpoint ?? throw new NullReferenceException("Cannot use null as a server endpoint.");
            return this;
        }

        public BuilderApi WithAccessToken(string accessToken)
        {
            AdminAccessToken = accessToken;
            return this;
        }
        
        public static implicit operator UpdateClientConfig(BuilderApi builder) => builder.Build();

        public UpdateClientConfig Build()
            => new()
            {
                ServerEndpoint = ServerEndpoint,
                Logger = Logger,
                AdminAccessToken = AdminAccessToken
            };
    }
}