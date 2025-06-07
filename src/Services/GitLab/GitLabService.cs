using System.Configuration;
using Gommon;
using NGitLab;

namespace RyujinxUpdate.Services.GitLab;

public class GitLabService
{
    public GitLabClient Client { get; }
    private readonly PeriodicTimer _refreshTimer;

    public GitLabService(IConfiguration config, ILogger<GitLabService> logger)
    {
        var gitlabSection = config.GetSection("GitLab");
        
        if (!gitlabSection.Exists())
            throw new ConfigurationErrorsException($"The '{gitlabSection.Key}' section does not exist in your appsettings.json. You need to provide an 'Endpoint', 'AccessToken', and optionally 'RefreshIntervalMinutes' values.");
        
        Client = new GitLabClient(
            config["GitLab:Endpoint"],
            config["GitLab:AccessToken"]
        );

        if (config["GitLab:RefreshIntervalMinutes"] is not { } refreshIntervalStr)
        {
            _refreshTimer = new(TimeSpan.FromMinutes(5));
            return;
        }
        
        if (int.TryParse(refreshIntervalStr, out var minutes))
            _refreshTimer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));
        else
        {
            logger.LogWarning(
                "Config value 'GitLab:RefreshIntervalSeconds' was not a valid integer. Defaulting to 5 minutes.");
            _refreshTimer = new(TimeSpan.FromMinutes(5));
        }
    }

    public void Init() => Executor.ExecuteBackgroundAsync(async () =>
    {
        
    });
}