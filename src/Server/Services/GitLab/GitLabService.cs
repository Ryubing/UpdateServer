using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using NGitLab;
using Ryujinx.Systems.Update.Server.Helpers.Http;

namespace Ryujinx.Systems.Update.Server.Services.GitLab;

public class GitLabService
{
    private static readonly GitLabReleaseJsonResponseSerializerContext ReleaseSerializerContext =
        new();

    private readonly IHttpClientProxy _http;
    public GitLabClient Client { get; }

    private readonly ILogger<GitLabService> _logger;

    public GitLabService(IConfiguration config, ILogger<GitLabService> logger, DefaultHttpClientProxy httpClient)
    {
        _logger = logger;

        var gitlabSection = config.GetSection("GitLab");

        if (!gitlabSection.Exists())
            throw new Exception(
                $"The '{gitlabSection.Key}' section does not exist in your appsettings.json. You need to provide an 'Endpoint', 'AccessToken', and optionally 'RefreshIntervalMinutes' values.");

        var host = gitlabSection.GetValue<string>("Endpoint")!.TrimEnd('/');
        var accessToken = gitlabSection.GetValue<string>("AccessToken");

        Client = new GitLabClient(host, accessToken);
        _http = httpClient;
    }

    private async ValueTask<T?> HandleNotFoundAsync<T>(HttpResponseMessage response, JsonTypeInfo<T> typeInfo)
    {
        var contentString = await response.Content.ReadAsStringAsync();

        if (contentString is """{"message":"404 Not Found"}""")
            return default;

        return JsonSerializer.Deserialize(contentString, typeInfo);
    }

    public Task<GitLabReleaseJsonResponse?> GetLatestReleaseAsync(long projectId)
        => GetReleaseAsync(projectId, "permalink/latest");

    public async Task<GitLabReleaseJsonResponse?> GetReleaseAsync(long projectId, string tagName) =>
        await HandleNotFoundAsync(
            await _http.GetAsync($"api/v4/projects/{projectId}/releases/{tagName}"),
            ReleaseSerializerContext.GitLabReleaseJsonResponse
        );

    public PaginatedEndpoint<GitLabReleaseJsonResponse> PageReleases(long projectId)
        => _http.Paginate<GitLabReleaseJsonResponse>(builder => builder
            .WithBaseUrl($"api/v4/projects/{projectId}/releases")
            .WithPerPageCount(100)
            .WithJsonContentParser(ReleaseSerializerContext.IEnumerableGitLabReleaseJsonResponse)
            .WithQueryStringParameters(
                QueryParams.Sort("desc"),
                QueryParams.OrderBy("created_at")
            )
        );
}