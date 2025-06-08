using System.Text.Json.Serialization;
using Gommon;
using NGitLab.Models;
using RyujinxUpdate.Model;

namespace RyujinxUpdate.Services.GitLab;

public class VersionCache : SafeDictionary<string, VersionCache.Entry>
{
    private readonly GitLabService _gl;
    private readonly ILogger<VersionCache> _logger;
    private readonly PeriodicTimer _refreshTimer;


    private (string Name, long Id)? _cachedProject;
    
    private string? _latestTag;

    public VersionCache(IConfiguration config, GitLabService gitlabService, ILogger<VersionCache> logger)
    {
        _gl = gitlabService;
        _logger = logger;
        
        if (config["GitLab:RefreshIntervalMinutes"] is not { } refreshIntervalStr)
        {
            _refreshTimer = new(TimeSpan.FromMinutes(5));
            return;
        }
        
        if (int.TryParse(refreshIntervalStr, out var minutes))
            _refreshTimer = new (TimeSpan.FromMinutes(minutes));
        else
        {
            logger.LogWarning(
                "Config value 'GitLab:RefreshIntervalSeconds' was not a valid integer. Defaulting to 5 minutes.");
            _refreshTimer = new(TimeSpan.FromMinutes(5));
        }
    }

    public void Init(ProjectId projectId) => Executor.ExecuteBackgroundAsync(async () =>
    {
        await Update(projectId);
        while (await _refreshTimer.WaitForNextTickAsync())
        {
            await Update(projectId);
        }
    });

    public Entry? Latest => this[_latestTag];

    public async Task Update(ProjectId projectId)
    {
        if (!_cachedProject.HasValue)
        {
            var project = await _gl.Client.Projects.GetAsync(projectId);

            _cachedProject = (project.NameWithNamespace, project.Id);
        }
        
        _logger.LogInformation("Reloading version cache for {project}", _cachedProject.Value.Name);
        
        _latestTag = (await _gl.GetLatestReleaseAsync(_cachedProject.Value.Id))?.TagName;

        if (_latestTag is null)
        {
            _logger.LogWarning("Latest version for {project} was a 404, aborting.", _cachedProject.Value.Name);
            return;
        }

        var releases = await _gl.GetReleasesAsync(_cachedProject.Value.Id);

        var temp = new List<Entry>();

        foreach (var release in releases)
        {
            var windowsX64 = release.Assets.Links.First(x => x.AssetName.ContainsIgnoreCase("win_x64"));
            //var windowsArm64 = release.Assets.Links.FirstOrDefault(x => x.Name.ContainsIgnoreCase("win_arm64"));
            var linuxX64 = release.Assets.Links.First(x =>
                x.AssetName.ContainsIgnoreCase("linux_x64") && !x.AssetName.EndsWithIgnoreCase(".AppImage"));
            var linuxX64AppImage =
                release.Assets.Links.First(x =>
                    x.AssetName.ContainsIgnoreCase("x64") && x.AssetName.EndsWithIgnoreCase(".AppImage"));
            var macOs = release.Assets.Links.First(x => x.AssetName.ContainsIgnoreCase("macos_universal"));
            var linuxArm64 = release.Assets.Links.First(x =>
                x.AssetName.ContainsIgnoreCase("linux_arm64") && !x.AssetName.EndsWithIgnoreCase(".AppImage"));
            var linuxArm64AppImage = release.Assets.Links.First(x =>
                x.AssetName.ContainsIgnoreCase("arm64") && x.AssetName.EndsWithIgnoreCase(".AppImage"));
            
            temp.Add(new Entry
            {
                Tag = release.TagName,
                Downloads =
                {
                    Windows =
                    {
                        X64 = windowsX64.Url,
                        Arm64 = string.Empty
                    },
                    Linux =
                    {
                        X64 = linuxX64.Url,
                        Arm64 = linuxArm64.Url
                    },
                    LinuxAppImage =
                    {
                        X64 = linuxX64AppImage.Url,
                        Arm64 = linuxArm64AppImage.Url
                    },
                    MacOS = macOs.Url
                }
            });
        }

        foreach (var entry in temp)
        {
            if (!ContainsKey(entry.Tag))
                Add(entry.Tag, entry);
        }
    }

    public class Entry
    {
        [JsonPropertyName("tag")]
        public string Tag { get; set; }
        
        [JsonPropertyName("downloads")] 
        public DownloadLinks Downloads { get; } = new();
    }
}