namespace Ryujinx.Systems.Update.Client;

public partial class UpdateClient
{
    public static BuilderApi Builder() => new();
    
    public class BuilderApi
    {
        private readonly UpdateClientConfig.BuilderApi _builder = new();

        public BuilderApi WithStdOutLogger()
        {
            _builder.WithStdOutLogger();
            return this;
        }
        
        public BuilderApi WithLogger(UpdateClientLogCallback callback)
        {
            _builder.WithLogger(callback);
            return this;
        }
        
        public BuilderApi WithServerEndpoint(string serverEndpoint)
        {
            _builder.WithServerEndpoint(serverEndpoint);
            return this;
        }

        public BuilderApi WithAccessToken(string accessToken)
        {
            _builder.WithAccessToken(accessToken);
            return this;
        }

        public static implicit operator UpdateClient(BuilderApi builder) => builder.Build();

        public UpdateClient Build() => new(_builder.Build());
    }
}