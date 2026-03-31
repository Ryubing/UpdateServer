using System.Diagnostics;
using ForgejoApiClient;
using ForgejoApiClient.Api;
using Gommon;
using Ryujinx.Systems.Update.Common;

namespace Ryujinx.Systems.Update.Server.Services.Forgejo;

public class ForgejoVersionCache : SafeDictionary<string, VersionCacheEntry>, IVersionCache
{
    private readonly ForgejoService _fj;
    private readonly ILogger<ForgejoVersionCache> _logger;
    private readonly PeriodicTimer? _refreshTimer;

    private Repository? _cachedProject;

    public bool HasProjectInfo => _cachedProject != null;

    public string ProjectName => _cachedProject!.full_name!;
    public long ProjectId => _cachedProject!.id!.Value;
    public string ProjectPath => _cachedProject!.full_name!;

    private string? _latestTag;

    private PinnedVersions _pinnedVersions = null!;

    PinnedVersions IVersionCache.PinnedVersions => _pinnedVersions; //late-init, see Init method

    // ReSharper disable once ReplaceWithFieldKeyword

    // ReleaseUrlFormat is a computed property; using the field keyword instead of this field
    // works and compiles, but it looks really wrong and I hate it.
    private readonly string _forgejoEndpoint;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ForgejoVersionCache(IConfiguration config, ForgejoService forgejoService,
        ILogger<ForgejoVersionCache> logger)
    {
        _fj = forgejoService;
        _logger = logger;

        _forgejoEndpoint = config["Forgejo:Endpoint"]!;

        if (config["Forgejo:RefreshIntervalMinutes"] is not { } refreshIntervalStr)
        {
            _refreshTimer = new(TimeSpan.FromMinutes(5));
            return;
        }

        if (int.TryParse(refreshIntervalStr, out var minutes))
        {
            if (minutes < 0)
            {
                logger.LogInformation(
                    "Config value 'Forgejo:RefreshIntervalSeconds' is a negative value. Disabling auto cache refreshes.");
                _refreshTimer = null;
            }
            else
                _refreshTimer = new(TimeSpan.FromMinutes(minutes));
        }
        else
        {
            logger.LogWarning(
                "Config value 'Forgejo:RefreshIntervalSeconds' was not a valid integer. Defaulting to 5 minutes.");
            _refreshTimer = new(TimeSpan.FromMinutes(5));
        }
    }

    public string ReleaseUrlFormat => $"{_forgejoEndpoint.TrimEnd('/')}/{ProjectPath}/releases/tag/{{0}}";

    public void Init(string projectId, PinnedVersions pinnedVersions) => Executor.ExecuteBackgroundAsync(async () =>
    {
        try
        {
            _cachedProject ??= await _fj.Client.Repository.GetAsync(projectId.Split('/')[0], projectId.Split('/')[1]);
        }
        catch (ForgejoClientException e)
        {
            _logger.LogError(
                "Encountered error when getting the project ({project}) for the version cache. Aborting. Error: {errorMessage}",
                projectId, e.Message);
            return;
        }

        _logger.LogInformation("Initializing version cache for {project}", ProjectName);

        _pinnedVersions = pinnedVersions;

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

    public VersionCacheEntry? GetLatest(SupportedPlatform platform, SupportedArchitecture arch)
    {
        if (!HasProjectInfo)
            return null;

        if (_pinnedVersions.Find(platform, arch) is { } pinnedVersion &&
            TryGetValue(pinnedVersion, out var pinnedLatest))
            return pinnedLatest;

        return Latest;
    }

    public async Task<VersionCacheEntry?> GetReleaseAsync(Func<ForgejoVersionCache, VersionCacheEntry?> getter)
    {
        using (await TakeLockAsync())
        {
            return getter(this);
        }
    }

    public async Task RefreshAsync()
    {
        _logger.LogInformation("Reloading version cache for {project}", ProjectName);

        try
        {
            _latestTag = await _fj.Client.Repository.GetReleaseLatestAsync(
                ProjectPath.Split('/')[0], ProjectPath.Split('/')[1]
            ).Then(r => r.tag_name);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Errored trying to get latest release for {project}; aborting. Message: {message}",
                ProjectName, e.Message);
            return;
        }

        if (_latestTag is null)
        {
            _logger.LogWarning("Latest version for {project} was a 404, aborting.", ProjectName);
            return;
        }

        var sw = Stopwatch.StartNew();

        var releases = await _fj.ListReleasesForRepositoryAsync(
            ProjectPath.Split('/')[0],
            ProjectPath.Split('/')[1]
        );

        var tempCacheEntries = releases.Select(release =>
            new VersionCacheEntry
            {
                Tag = release.TagName,
                ReleaseUrl = ReleaseUrlFormat.Format(release.TagName),
                Downloads = new DownloadLinks
                {
                    Windows = new DownloadLinks.SupportedPlatform
                    {
                        X64 = release.Assets?
                            .FirstOrDefault(x => x.Name.ContainsIgnoreCase("win_x64"))
                            ?.DownloadUrl ?? string.Empty,
                        Arm64 = release.Assets?
                            .FirstOrDefault(x => x.Name.ContainsIgnoreCase("win_arm64"))
                            ?.DownloadUrl ?? string.Empty
                    },
                    Linux = new DownloadLinks.SupportedPlatform
                    {
                        X64 = release.Assets?
                            .FirstOrDefault(x => x.Name.ContainsIgnoreCase("linux_x64"))
                            ?.DownloadUrl ?? string.Empty,
                        Arm64 = release.Assets?
                            .FirstOrDefault(x => x.Name.ContainsIgnoreCase("linux_arm64"))
                            ?.DownloadUrl ?? string.Empty
                    },
                    LinuxAppImage = new DownloadLinks.SupportedPlatform
                    {
                        X64 = release.Assets?
                            .FirstOrDefault(x => x.Name.EndsWithIgnoreCase("x64.AppImage"))
                            ?.DownloadUrl ?? string.Empty,
                        Arm64 = release.Assets?
                            .FirstOrDefault(x => x.Name.EndsWithIgnoreCase("arm64.AppImage"))
                            ?.DownloadUrl ?? string.Empty
                    },
                    MacOS = release.Assets?
                        .FirstOrDefault(x =>
                            x.Name.ContainsIgnoreCase("macos_universal") ||
                            x.Name.ContainsIgnoreCase("macos_arm64"))
                        ?.DownloadUrl ?? string.Empty
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
        var versionCacheSection = app.Configuration.GetSection("Forgejo")
            .GetRequiredSection("VersionCacheSources");

        var stableSource = versionCacheSection.GetValue<string>("Stable");

        if (stableSource is null)
            throw new Exception(
                "Cannot start the server without a Forgejo repository in Forgejo:VersionCacheSources:Stable");

        var vpSection = app.Configuration.GetSection("VersionPinning");

        var pvLogger = app.Services.Get<ILoggerFactory>().CreateLogger<PinnedVersions>();

        var stableCache = app.Services.GetRequiredKeyedService<ForgejoVersionCache>("stableCache");
        stableCache.Init(stableSource,
            new PinnedVersions(pvLogger, vpSection.GetSection("Stable")));

        var canarySource = versionCacheSection.GetValue<string>("Canary");

        if (canarySource != null)
        {
            var canaryCache = app.Services.GetRequiredKeyedService<ForgejoVersionCache>("canaryCache");
            canaryCache.Init(canarySource,
                new PinnedVersions(pvLogger, vpSection.GetSection("Canary")));
        }

        var custom1Source = versionCacheSection.GetValue<string>("Custom1");

        if (custom1Source != null)
        {
            var canaryCache = app.Services.GetRequiredKeyedService<ForgejoVersionCache>("custom1Cache");
            canaryCache.Init(custom1Source,
                new PinnedVersions(pvLogger, vpSection.GetSection("Custom1")));
        }

        var kenjiNxSource = versionCacheSection.GetValue<string>("KenjiNX");

        if (kenjiNxSource != null)
        {
            var kenjiCache = app.Services.GetRequiredKeyedService<ForgejoVersionCache>("kenjinxCache");
            kenjiCache.Init(kenjiNxSource,
                new PinnedVersions(pvLogger, vpSection.GetSection("KenjiNX")));
        }
    }
}