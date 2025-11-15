namespace Ryujinx.Systems.Update.Common;

public enum SupportedPlatform
{
    Windows,
    Linux,
    LinuxAppImage,
    Mac
}

public static partial class EnumExtensions
{
    extension(SupportedPlatform platform)
    {
        public string QueryStringValue => platform switch
        {
            SupportedPlatform.Windows => "win",
            SupportedPlatform.Linux => "linux",
            SupportedPlatform.LinuxAppImage => "linuxappimage",
            SupportedPlatform.Mac => "mac",
            _ => throw new ArgumentOutOfRangeException(nameof(platform))
        };
    }

    public static bool TryParseAsSupportedPlatform(this string? os, out SupportedPlatform platform)
    {
        platform = default;
        SupportedPlatform? temp = os?.ToLower() switch
        {
            "w" or "win" or "windows" => SupportedPlatform.Windows,
            "l" or "lin" or "linux" => SupportedPlatform.Linux,
            "ai" or "appimage" or "linuxappimage" or "linuxai" => SupportedPlatform.LinuxAppImage,
            "m" or "mac" or "osx" or "macos" => SupportedPlatform.Mac,
            _ => null
        };

        if (!temp.HasValue)
            return false;

        platform = temp.Value;
        return true;
    }
}