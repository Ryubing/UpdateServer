using Gommon;
using NGitLab.Models;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Updater.Common;

namespace Ryujinx.Systems.Updater.Server.Services.GitLab;

public class VersionCache : SafeDictionary<string, VersionCacheEntry>
{
    private readonly GitLabService _gl;
    private readonly ILogger<VersionCache> _logger;
    private readonly PeriodicTimer? _refreshTimer;

    private (string Name, long Id, string Path)? _cachedProject;
    
    private string? _latestTag;

    private readonly string _gitlabEndpoint;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public VersionCache(IConfiguration config, GitLabService gitlabService, ILogger<VersionCache> logger)
    {
        _gl = gitlabService;
        _logger = logger;

        _gitlabEndpoint = config["GitLab:Endpoint"]!;
        
        if (config["GitLab:RefreshIntervalMinutes"] is not { } refreshIntervalStr)
        {
            _refreshTimer = new(TimeSpan.FromMinutes(5));
            return;
        }

        if (int.TryParse(refreshIntervalStr, out var minutes))
        {
            if (minutes < 0)
            {
                logger.LogInformation("Config value 'GitLab:RefreshIntervalSeconds' is a negative value. Disabling auto cache refreshes.");
                _refreshTimer = null;
            }
            else
                _refreshTimer = new (TimeSpan.FromMinutes(minutes));
        }
        else
        {
            logger.LogWarning(
                "Config value 'GitLab:RefreshIntervalSeconds' was not a valid integer. Defaulting to 5 minutes.");
            _refreshTimer = new(TimeSpan.FromMinutes(5));
        }
    }

    public string FormatReleaseUrlFormat() => $"{_gitlabEndpoint.TrimEnd('/')}/{_cachedProject!.Value.Path}/-/releases/{{0}}";
    public string FormatReleaseUrl(string tag) => $"{_gitlabEndpoint.TrimEnd('/')}/{_cachedProject!.Value.Path}/-/releases/{tag}";

    public void Init(ProjectId projectId) => Executor.ExecuteBackgroundAsync(async () =>
    {
        if (!_cachedProject.HasValue)
        {
            var project = await _gl.Client.Projects.GetAsync(projectId);

            _cachedProject = (project.NameWithNamespace, project.Id, project.PathWithNamespace);
        }
        
        await Update();
        while (_refreshTimer != null && await _refreshTimer.WaitForNextTickAsync())
        {
            await Update();
        }
    });

    public Task<Extensions.ScopedSemaphoreLock> TakeLockAsync() => _semaphore.TakeAsync();

    public VersionCacheEntry? Latest => this[_latestTag ?? string.Empty];

    public async Task Update()
    {
        await _semaphore.WaitAsync();
        
        _logger.LogInformation("Reloading version cache for {project}", _cachedProject!.Value.Name);
        
        _latestTag = (await _gl.GetLatestReleaseAsync(_cachedProject.Value.Id))?.TagName;

        if (_latestTag is null)
        {
            _logger.LogWarning("Latest version for {project} was a 404, aborting.", _cachedProject.Value.Name);
            return;
        }

        var releases = await _gl.GetReleasesAsync(_cachedProject.Value.Id);
        
        Clear();

        foreach (var release in releases)
        {
            this[release.TagName] = new VersionCacheEntry
            {
                Tag = release.TagName,
                ReleaseUrl = FormatReleaseUrl(release.TagName),
                Downloads =
                {
                    Windows =
                    {
                        X64 = release.Assets.Links.First(x => x.AssetName.ContainsIgnoreCase("win_x64")).Url,
                        Arm64 = string.Empty
                    },
                    Linux =
                    {
                        X64 = release.Assets.Links.First(x => x.AssetName.ContainsIgnoreCase("linux_x64")).Url,
                        Arm64 = release.Assets.Links.First(x => x.AssetName.ContainsIgnoreCase("linux_arm64")).Url
                    },
                    LinuxAppImage =
                    {
                        X64 = release.Assets.Links.First(x => x.AssetName.EndsWithIgnoreCase("x64.AppImage")).Url,
                        Arm64 = release.Assets.Links.First(x => x.AssetName.EndsWithIgnoreCase("arm64.AppImage")).Url
                    },
                    MacOS = release.Assets.Links.First(x => x.AssetName.ContainsIgnoreCase("macos_universal")).Url
                }
            };
        }

        _semaphore.Release();
    }
}