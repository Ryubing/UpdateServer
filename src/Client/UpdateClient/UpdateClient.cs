using System.Runtime.CompilerServices;

namespace Ryujinx.Systems.Update.Client;

/// <summary>
///     An HTTP REST API wrapper around Ryujinx.Systems.Update.Server.
/// </summary>
public partial class UpdateClient : IDisposable
{
    private readonly UpdateClientConfig _config;
    private readonly HttpClient _http;
    
    /// <summary>
    ///     Create a new <see cref="UpdateClient"/> with a given configuration
    /// </summary>
    /// <param name="config">The <see cref="UpdateClient"/> configuration, including where log messages go, the server to use, and the optional Admin access token.</param>
    /// <exception cref="NullReferenceException">Thrown when the supplied server endpoint on <see cref="UpdateClientConfig"/> is null.</exception>
    public UpdateClient(UpdateClientConfig config)
    {
        _config = config;
        _http = new HttpClient
        {
            BaseAddress = new Uri(_config.ServerEndpoint ??
                                  throw new NullReferenceException(
                                      "Cannot create an UpdateClient with no server endpoint."))
        };
    }

    void IDisposable.Dispose()
    {
        _http.Dispose();
        GC.SuppressFinalize(this);
    }
    
    private void Log(string format, IEnumerable<object> args, [CallerMemberName] string caller = null!) 
        => _config.Logger((format, caller), args.ToArray());
    
    private void ApplyAuthorization(HttpRequestMessage httpRequest)
    {
        httpRequest.Headers.Add("Authorization", _config.AdminAccessToken);
    }
}