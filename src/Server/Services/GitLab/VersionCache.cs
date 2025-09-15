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

    private Project? _cachedProject;

    public bool HasProjectInfo => _cachedProject != null;

    public string ProjectName => _cachedProject!.NameWithNamespace;
    public long ProjectId => _cachedProject!.Id;
    public string ProjectPath => _cachedProject!.PathWithNamespace;

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

    public string ReleaseUrlFormat => $"{_gitlabEndpoint.TrimEnd('/')}/{ProjectPath}/-/releases/{{0}}";

    public void Init(ProjectId projectId) => Executor.ExecuteBackgroundAsync(async () =>
    {
        try
        {
            _cachedProject ??= await _gl.Client.Projects.GetAsync(projectId);
        }
        catch (GitLabException e)
        {
            _logger.LogError(
                "Encountered error when getting the project ({project}) for the version cache. Aborting. Error: {errorMessage}",
                projectId.ValueAsString(), e.ErrorMessage);
            return;
        }

        _logger.LogInformation("Initializing version cache for {project}", ProjectName);

        await RefreshAsync();

        if (_refreshTimer == null)
        {
            string howToRefresh = AdminEndpointMetadata.Enabled
                ? $"using the {Constants.FullRouteName_Api_Admin_RefreshCache} endpoint or restarting the server."
                : "restarting the server. Set an admin access token in appsettings.json to enable an endpoint to do this.";

            _logger.LogInformation(
                "Periodic version cache refreshing is disabled for {project}. It can be refreshed by {means}",
                ProjectName, howToRefresh);
            return;
        }

        _logger.LogInformation("Refreshing version cache for {project} every {timePeriod} minutes.",
            ProjectName, _refreshTimer.Period.TotalMinutes);
        while (await _refreshTimer.WaitForNextTickAsync())
        {
            await RefreshAsync();
        }
    });

    public Task<IDisposable> TakeLockAsync() => _semaphore.TakeAsync();

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
        _logger.LogInformation("Reloading version cache for {project}", ProjectName);

        _latestTag = (await _gl.GetLatestReleaseAsync(ProjectId))?.TagName;

        if (_latestTag is null)
        {
            _logger.LogWarning("Latest version for {project} was a 404, aborting.", ProjectName);
            return;
        }

        var sw = Stopwatch.StartNew();

        var releases = await _gl.PageReleases(ProjectId)
            .GetAllAsync(onNonSuccess:
                code => _logger.LogError(
                    "One of the pagination requests to get all releases returned a non-success status code: {code}",
                    Enum.GetName(code) ?? $"{(int)code}")
            );

        if (releases is null)
            return;

        var tempCacheEntries = releases.Select(release =>
            new VersionCacheEntry
            {
                Tag = release.TagName,
                ReleaseUrl = ReleaseUrlFormat.Format(release.TagName),
                Downloads =
                {
                    Windows =
                    {
                        X64 = release.Assets.Links
                            .FirstOrDefault(x => x.AssetName.ContainsIgnoreCase("win_x64"))
                            ?.Url ?? string.Empty,
                        Arm64 = release.Assets.Links
                            .FirstOrDefault(x => x.AssetName.ContainsIgnoreCase("win_arm64"))
                            ?.Url ?? string.Empty
                    },
                    Linux =
                    {
                        X64 = release.Assets.Links
                            .FirstOrDefault(x => x.AssetName.ContainsIgnoreCase("linux_x64"))
                            ?.Url ?? string.Empty,
                        Arm64 = release.Assets.Links
                            .FirstOrDefault(x => x.AssetName.ContainsIgnoreCase("linux_arm64"))
                            ?.Url ?? string.Empty
                    },
                    LinuxAppImage =
                    {
                        X64 = release.Assets.Links
                            .FirstOrDefault(x => x.AssetName.EndsWithIgnoreCase("x64.AppImage"))
                            ?.Url ?? string.Empty,
                        Arm64 = release.Assets.Links
                            .FirstOrDefault(x => x.AssetName.EndsWithIgnoreCase("arm64.AppImage"))
                            ?.Url ?? string.Empty
                    },
                    MacOS = release.Assets.Links
                        .FirstOrDefault(x => x.AssetName.ContainsIgnoreCase("macos_universal"))
                        ?.Url ?? string.Empty
                }
            }).ToDictionary(x => x.Tag, x => x);
        
        await _semaphore.WaitAsync();
        
        if (Count > 0)
        {
            _logger.LogInformation("Clearing {entryCount} version cache entries for {project}", Count,
                ProjectName);
            Clear();
        }

        foreach (var (tag, entry) in tempCacheEntries)
        {
            _logger.LogTrace("Adding version cache entry {tag} for {project}", tag, ProjectName);

            this[tag] = entry;
        }
        
        tempCacheEntries.Clear();

        sw.Stop();
        
        _semaphore.Release();

        _logger.LogInformation("Loaded {entryCount} version cache entries for {project}; took {time}ms.", Count,
            ProjectName, sw.ElapsedMilliseconds);
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