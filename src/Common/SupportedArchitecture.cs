namespace Ryujinx.Systems.Update.Common;

public enum SupportedArchitecture
{
    Amd64,
    Arm64
}

public static partial class EnumExtensions
{
    public static string AsQueryStringValue(this SupportedArchitecture arch) =>
        Enum.GetName(arch)?.ToLower() ?? throw new ArgumentOutOfRangeException(nameof(arch));
    
    public static SupportedArchitecture? TryParseAsSupportedArchitecture(this string? rc) => rc?.ToLower() switch
    {
        "amd64" or "x64" or "x86-64" or "x86_64" => SupportedArchitecture.Amd64,
        "arm64" or "a64" or "arm" => SupportedArchitecture.Arm64,
        _ => null
    };
}