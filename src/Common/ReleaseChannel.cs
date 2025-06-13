namespace Ryujinx.Systems.Update.Common;

public enum ReleaseChannel
{
    Stable,
    Canary
}

public static partial class EnumExtensions
{
    public static string AsQueryStringValue(this ReleaseChannel rc) =>
        Enum.GetName(rc)?.ToLower() ?? throw new ArgumentOutOfRangeException(nameof(rc));
    
    public static ReleaseChannel? TryParseAsReleaseChannel(this string? rc) => rc?.ToLower() switch
    {
        "stable" => ReleaseChannel.Stable,
        "canary" => ReleaseChannel.Canary,
        _ => null
    };
}