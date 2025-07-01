using System.Diagnostics;
using Gommon;
using NGitLab;
using NGitLab.Models;
using Ryujinx.Systems.Update.Common;

namespace Ryujinx.Systems.Update.Server.Services.GitLab;

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
                logger.LogInformation(
                    "Config value 'GitLab:RefreshIntervalSeconds' is a negative value. Disabling auto cache refreshes.");
                _refreshTimer = null;
            }
            else
                _refreshTimer = new(TimeSpan.FromMinutes(minutes));
        }
        else
        {
            logger.LogWarning(
                "Config value 'GitLab:RefreshIntervalSeconds' was not a valid integer. Defaulting to 5 minutes.");
            _refreshTimer = new(TimeSpan.FromMinutes(5));
        }
    }

    public string ReleaseUrlFormat => $"{_gitlabEndpoint.TrimEnd('/')}/{_cachedProject!.Value.Path}/-/releases/{{0}}";

    public void Init(ProjectId projectId) => Executor.ExecuteBackgroundAsync(async () =>
    {
        try
        {
            if (!_cachedProject.HasValue)
            {
                var project = await _gl.Client.Projects.GetAsync(projectId);

                _cachedProject = (project.NameWithNamespace, project.Id, project.PathWithNamespace);
            }
        }
        catch (GitLabException e)
        {
            _logger.LogError(
                "Encountered error when getting the project ({project}) for the version cache. Aborting. Error: {errorMessage}",
                projectId.ValueAsString(), e.ErrorMessage);
            return;
        }

        _logger.LogInformation("Initializing version cache for {project}", _cachedProject!.Value.Name);

        await RefreshAsync();

        if (_refreshTimer == null)
        {
            string howToRefresh = AdminEndpointMetadata.Enabled
                ? $"using the {Constants.FullRouteName_Api_Admin_RefreshCache} endpoint or restarting the server."
                : "restarting the server. There is an admin-only endpoint available that has not been configured. Set an admin access token in appsettings.json to enable the endpoint.";

            _logger.LogInformation(
                "Periodic version cache refreshing is disabled for {project}. It can be refreshed by {means}",
                _cachedProject!.Value.Name, howToRefresh);
            return;
        }

        _logger.LogInformation("Refreshing version cache for {project} every {timePeriod} minutes.",
            _cachedProject!.Value.Name, _refreshTimer.Period.TotalMinutes);
        while (await _refreshTimer.WaitForNextTickAsync())
        {
            await RefreshAsync();
        }
    });

    public Task<Gommon.Extensions.ScopedSemaphoreLock> TakeLockAsync() => _semaphore.TakeAsync();

    public VersionCacheEntry? Latest => this[_latestTag ?? string.Empty];

    public async Task<VersionCacheEntry?> GetReleaseAsync(Func<VersionCache, VersionCacheEntry?> getter)
    {
        using (await TakeLockAsync())
        {
            return getter(this);
        }
    }

    public async Task RefreshAsync()
    {
        await _semaphore.WaitAsync();

        _logger.LogInformation("Reloading version cache for {project}", _cachedProject!.Value.Name);

        _latestTag = (await _gl.GetLatestReleaseAsync(_cachedProject.Value.Id))?.TagName;

        if (_latestTag is null)
        {
            _logger.LogWarning("Latest version for {project} was a 404, aborting.", _cachedProject.Value.Name);
            return;
        }

        _logger.LogInformation("Clearing {entryCount} version cache entries for {project}", Count,
            _cachedProject!.Value.Name);

        var sw = Stopwatch.StartNew();

        var releases = await _gl.PageReleases(_cachedProject.Value.Id)
            .GetAllAsync(onNonSuccess:
                code => _logger.LogError(
                    "One of the pagination requests to get all releases returned a non-success status code: {code}",
                    Enum.GetName(code) ?? $"{(int)code}")
            );

        if (releases == null)
            goto ReleaseLock;

        Clear();

        foreach (var release in releases)
        {
            _logger.LogTrace("Adding version cache entry {tag} for {project}", release.TagName, _cachedProject!.Value.Name);
            this[release.TagName] = new VersionCacheEntry
            {
                Tag = release.TagName,
                ReleaseUrl = ReleaseUrlFormat.Format(release.TagName),
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

        sw.Stop();

        _logger.LogInformation("Loaded {entryCount} version cache entries for {project}; took {time}ms.", Count,
            _cachedProject!.Value.Name, sw.ElapsedMilliseconds);

    ReleaseLock:
        _semaphore.Release();
    }

    public static void InitializeVersionCaches(WebApplication app)
    {
        var versionCacheSection = app.Configuration.GetSection("GitLab").GetRequiredSection("VersionCacheSources");

        var stableSource = versionCacheSection.GetValue<string>("Stable");

        if (stableSource is null)
            throw new Exception(
                "Cannot start the server without a GitLab repository in GitLab:VersionCacheSources:Stable");

        app.Services.GetRequiredKeyedService<VersionCache>("stableCache").Init(new ProjectId(stableSource));

        var canarySource = versionCacheSection.GetValue<string>("Canary");

        if (canarySource != null)
            app.Services.GetRequiredKeyedService<VersionCache>("canaryCache").Init(new ProjectId(canarySource));
    }
}