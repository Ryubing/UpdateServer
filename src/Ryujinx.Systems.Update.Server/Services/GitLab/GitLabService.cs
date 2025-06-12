using System.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using NGitLab;

namespace RyujinxUpdate.Services.GitLab;

public class GitLabService
{
    private static readonly GitLabReleaseJsonResponseSerializerContext ReleaseSerializerContext =
        new();
    
    private readonly HttpClient _http;
    public GitLabClient Client { get; }

    private readonly ILogger<GitLabService> _logger;

    public GitLabService(IConfiguration config, ILogger<GitLabService> logger)
    {
        _logger = logger;
        
        var gitlabSection = config.GetSection("GitLab");
        
        if (!gitlabSection.Exists())
            throw new Exception($"The '{gitlabSection.Key}' section does not exist in your appsettings.json. You need to provide an 'Endpoint', 'AccessToken', and optionally 'RefreshIntervalMinutes' values.");

        var host = gitlabSection.GetValue<string>("Endpoint")!.TrimEnd('/');
        var accessToken = gitlabSection.GetValue<string>("AccessToken");
        
        Client = new GitLabClient(host, accessToken);
        _http = new HttpClient
        {
            BaseAddress = new Uri(host),
            DefaultRequestHeaders = { Authorization = AuthenticationHeaderValue.Parse($"Bearer {accessToken}") }
        };
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
    
    public async Task<GitLabReleaseJsonResponse[]> GetReleasesAsync(long projectId) =>
        (await _http.GetFromJsonAsync(
            $"api/v4/projects/{projectId}/releases", 
            ReleaseSerializerContext.GitLabReleaseJsonResponseArray))!;
}