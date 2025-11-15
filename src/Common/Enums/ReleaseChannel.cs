namespace Ryujinx.Systems.Update.Common;

public enum ReleaseChannel
{
    Stable,
    Canary
}

public static partial class EnumExtensions
{
    extension(ReleaseChannel rc)
    {
        public string QueryStringValue => Enum.GetName(rc)?.ToLower() ?? throw new ArgumentOutOfRangeException(nameof(rc));
    }

    public static bool TryParseAsReleaseChannel(this string? rawRc, out ReleaseChannel rc)
    {
        rc = default;
        ReleaseChannel? tempRc = rawRc?.ToLower() switch
        {
            "stable" => ReleaseChannel.Stable,
            "canary" => ReleaseChannel.Canary,
            _ => null
        };

        if (!tempRc.HasValue)
            return false;

        rc = tempRc.Value;
        return true;
    }
}