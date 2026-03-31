using System.Diagnostics.CodeAnalysis;
using Ryujinx.Systems.Update.Common;

namespace Ryujinx.Systems.Update.Server.Services;

public interface IVersionCache
{
    public bool HasProjectInfo { get; }

    public string ProjectName { get; }

    public long ProjectId { get; }
    public string ProjectPath { get; }

    public PinnedVersions PinnedVersions { get; } //late-init, see Init method

    public string ReleaseUrlFormat { get; }

    public void Init(string projectPath, PinnedVersions pinnedVersions);

    public Task<IDisposable> TakeLockAsync();

    public VersionCacheEntry? Latest { get; }

    public VersionCacheEntry? GetLatest(SupportedPlatform platform, SupportedArchitecture arch)
    {
        if (!HasProjectInfo)
            return null;

        if (PinnedVersions.Find(platform, arch) is { } pinnedVersion &&
            TryGetValue(pinnedVersion, out var pinnedLatest))
            return pinnedLatest;

        return Latest;
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out VersionCacheEntry value);

    public async Task<VersionCacheEntry?> GetReleaseAsync(Func<IVersionCache, VersionCacheEntry?> getter)
    {
        using (await TakeLockAsync())
        {
            return getter(this);
        }
    }

    public Task RefreshAsync();
}