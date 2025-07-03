using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using NGitLab;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Update.Server.Helpers.Http;

namespace Ryujinx.Systems.Update.Server.Services.GitLab;

public class GitLabService
{
    private readonly IHttpClientProxy _http;
    public GitLabClient Client { get; }

    public GitLabService(IConfiguration config, DefaultHttpClientProxy httpClient)
    {
        var gitlabSection = config.GetSection("GitLab");

        if (!gitlabSection.Exists())
            throw new Exception(
                $"The '{gitlabSection.Key}' section does not exist in your appsettings.json. You need to provide an 'Endpoint', 'AccessToken', and optionally 'RefreshIntervalMinutes' values.");

        var host = gitlabSection.GetValue<string>("Endpoint")!.TrimEnd('/');
        var accessToken = gitlabSection.GetValue<string>("AccessToken");

        Client = new GitLabClient(host, accessToken);
        _http = httpClient;
    }

    private async ValueTask<T?> HandleNotFoundAsync<T>(HttpResponseMessage response, JsonTypeInfo<T> typeInfo) where T : class
    {
        var contentString = await response.Content.ReadAsStringAsync();

        if (contentString is """{"message":"404 Not Found"}""")
            return null;

        return JsonSerializer.Deserialize(contentString, typeInfo);
    }

    public Task<GitLabReleaseJsonResponse?> GetLatestReleaseAsync(long projectId)
        => GetReleaseAsync(projectId, "permalink/latest");

    public async Task<GitLabReleaseJsonResponse?> GetReleaseAsync(long projectId, string tagName) =>
        await HandleNotFoundAsync(
            await _http.GetAsync($"api/v4/projects/{projectId}/releases/{tagName}"),
            JsonSerializerContexts.Default.GitLabReleaseJsonResponse
        );

    public PaginatedEndpoint<GitLabReleaseJsonResponse> PageReleases(long projectId)
        => _http.Paginate<GitLabReleaseJsonResponse>(builder => builder
            .WithBaseUrl($"api/v4/projects/{projectId}/releases")
            .WithPerPageCount(100)
            .WithJsonContentParser(JsonSerializerContexts.Default.IEnumerableGitLabReleaseJsonResponse)
            .WithQueryStringParameters(
                QueryParams.Sort("desc"),
                QueryParams.OrderBy("created_at")
            )
        );
}