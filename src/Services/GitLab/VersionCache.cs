using Gommon;
using NGitLab.Models;

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

    public void Init(ProjectId projectId) => Executor.Execute(async () =>
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
                    WindowsX64 = windowsX64.Url,
                    LinuxX64 = linuxX64.Url,
                    LinuxAppImageX64 = linuxX64AppImage.Url,
                    MacOsUniversal = macOs.Url,
                    LinuxArm64 = linuxArm64.Url,
                    LinuxAppImageArm64 = linuxArm64AppImage.Url
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
        public string Tag { get; set; }
        public DownloadLinks Downloads { get; } = new();

        public class DownloadLinks
        {
            public string WindowsX64 { get; set; }
            //public string WindowsArm64 { get; set; }
            public string LinuxX64 { get; set; }
            public string LinuxArm64 { get; set; }
            public string LinuxAppImageX64 { get; set; }
            public string LinuxAppImageArm64 { get; set; }
            public string MacOsUniversal { get; set; }
        }
    }
}