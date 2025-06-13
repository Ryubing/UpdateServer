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
    public static string AsQueryStringValue(this SupportedPlatform supportedPlatform) => supportedPlatform switch
    {
        SupportedPlatform.Windows => "win",
        SupportedPlatform.Linux => "linux",
        SupportedPlatform.Mac => "mac",
        _ => throw new ArgumentOutOfRangeException(nameof(supportedPlatform))
    };
    
    public static SupportedPlatform? TryParseAsSupportedPlatform(this string? os) => os?.ToLower() switch
    {
        "w" or "win" or "windows" => SupportedPlatform.Windows,
        "l" or "lin" or "linux" => SupportedPlatform.Linux,
        "ai" or "appimage" or "linuxappimage" or "linuxai" => SupportedPlatform.LinuxAppImage,
        "m" or "mac" or "osx" or "macos" => SupportedPlatform.Mac, 
        _ => null
    };
}