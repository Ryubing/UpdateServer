using ForgejoApiClient;
using Gommon;
using Ryujinx.Systems.Update.Server.Helpers.Http;

namespace Ryujinx.Systems.Update.Server.Services.Forgejo;

public class ForgejoService
{
    private readonly IHttpClientProxy _http;
    public ForgejoClient Client { get; }

    public ForgejoService(IConfiguration config, ILoggerFactory loggerFactory)
    {
        var fjSection = config.GetSection("Forgejo");

        if (!fjSection.Exists())
            throw new Exception(
                $"The '{fjSection.Key}' section does not exist in your appsettings.json. You need to provide an 'Endpoint', 'AccessToken', and optionally 'RefreshIntervalMinutes' values.");

        var host = fjSection.GetValue<string>("Endpoint")!.TrimEnd('/');
        var accessToken = fjSection.GetValue<string>("AccessToken");

        Client = new ForgejoClient(new Uri(host), accessToken!);
        _http = new DefaultHttpClientProxy(config, loggerFactory.CreateLogger<IHttpClientProxy>());
    }

    public Task<Release[]> ListReleasesForRepositoryAsync(string owner, string repo) =>
        _http.Paginate<Release>(builder => builder
            .WithBaseUrl($"api/v1/repos/{owner}/{repo}/releases")
            .WithJsonContentParser(ServerJsonSerializerContexts.Default.IEnumerableRelease)
        ).GetAllAsync().Then(x => x!.ToArray());
}