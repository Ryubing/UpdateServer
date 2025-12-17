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
            _ => throw new ArgumentOutOfRangeException()
        };

    public string GetNextVersion(ReleaseChannel rc) =>
        rc switch
        {
            ReleaseChannel.Stable => _data.Stable.CopyIncrement().ToString(),
            ReleaseChannel.Canary => _data.Canary.CopyIncrement().ToString(),
            _ => throw new ArgumentOutOfRangeException()
        };

    public VersionProvider.Entry IncrementBuild(ReleaseChannel rc)
    {
        var entry = rc switch
        {
            ReleaseChannel.Stable => _data.Stable = _data.Stable.CopyIncrement(),
            ReleaseChannel.Canary => _data.Canary = _data.Canary.CopyIncrement(),
            _ => throw new ArgumentOutOfRangeException()
        };

        _data.Save();

        return entry;
    }

    public void Advance()
    {
        _data.IncrementAndReset();
    }
}