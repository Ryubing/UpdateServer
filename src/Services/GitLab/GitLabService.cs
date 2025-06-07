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
            throw new ConfigurationErrorsException($"The '{gitlabSection.Key}' section does not exist in your appsettings.json. You need to provide an 'Endpoint', 'AccessToken', and optionally 'RefreshIntervalMinutes' values.");
        
        Client = new GitLabClient(
            config["GitLab:Endpoint"],
            config["GitLab:AccessToken"]
        );

        _http = new HttpClient
        {
            BaseAddress = new Uri(gitlabSection.GetValue<string>("Endpoint")!.TrimEnd('/')),
            DefaultRequestHeaders =
            {
                Authorization =
                    AuthenticationHeaderValue.Parse($"Bearer {gitlabSection.GetValue<string>("AccessToken")}")
            }
        };
    }

    private async ValueTask<T?> HandleNotFoundAsync<T>(HttpResponseMessage response, JsonTypeInfo<T> typeInfo)
    {
        var contentString = await response.Content.ReadAsStringAsync();

        if (contentString is """{"message":"404 Not Found"}""")
            return default;
        
        _logger.LogInformation(contentString);

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