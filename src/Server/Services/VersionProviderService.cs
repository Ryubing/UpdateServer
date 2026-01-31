using System.Text.Json;
using Ryujinx.Systems.Update.Common;

namespace Ryujinx.Systems.Update.Server.Services;

public class VersionProviderService
{
    private readonly VersionProvider _data;

    public VersionProviderService(ILogger<VersionProviderService> logger)
    {
        try
        {
            if (VersionProvider.Read() is { } deserialized)
                _data = deserialized;
        }
        catch (JsonException je)
        {
            logger.LogError(je, "Invalid JSON in version provider configuration");
        }
    }

    public ulong CurrentMajor => _data.Stable.Major;

    public string GetCurrentVersion(ReleaseChannel rc) =>
        rc switch
        {
            ReleaseChannel.Stable => _data.Stable.ToString(),
            ReleaseChannel.Canary => _data.Canary.ToString(),
            ReleaseChannel.Custom1 => _data.Custom1.ToString(),
            _ => throw new ArgumentOutOfRangeException()
        };

    public string GetNextVersion(ReleaseChannel rc, bool isMajorRelease = false) =>
        rc switch
        {
            ReleaseChannel.Stable => (
                isMajorRelease
                    ? _data.Stable.NextMajor()
                    : _data.Stable.NextBuild()
            ).ToString(),
            ReleaseChannel.Canary => _data.Canary.NextBuild().ToString(), // canaries cannot move the major release, so ignore that boolean for this branch.
            ReleaseChannel.Custom1 => (
                isMajorRelease
                    ? _data.Custom1.NextMajor()
                    : _data.Custom1.NextBuild()
            ).ToString(),
            _ => throw new ArgumentOutOfRangeException()
        };

    public VersionProvider.Entry IncrementBuild(ReleaseChannel rc)
    {
        var entry = rc switch
        {
            ReleaseChannel.Stable => _data.Stable = _data.Stable.NextBuild(),
            ReleaseChannel.Canary => _data.Canary = _data.Canary.NextBuild(),
            ReleaseChannel.Custom1 => _data.Custom1 = _data.Custom1.NextBuild(),
            _ => throw new ArgumentOutOfRangeException()
        };

        _data.Save();

        return entry;
    }

    public void Advance(ReleaseChannel rc)
    {
        _data.IncrementAndReset(rc);
    }
}