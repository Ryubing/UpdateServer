namespace Ryujinx.Systems.Update.Common;

public enum SupportedArchitecture
{
    Amd64,
    Arm64
}

public static partial class EnumExtensions
{
    extension(SupportedArchitecture arch)
    {
        public string QueryStringValue =>
            Enum.GetName(arch)?.ToLower() ?? throw new ArgumentOutOfRangeException(nameof(arch));
    }

    public static bool TryParseAsSupportedArchitecture(this string? rawArch, out SupportedArchitecture arch)
    {
        arch = default;
        SupportedArchitecture? temp = rawArch?.ToLower() switch
        {
            "amd64" or "x64" or "x86-64" or "x86_64" => SupportedArchitecture.Amd64,
            "arm64" or "a64" or "arm" => SupportedArchitecture.Arm64,
            _ => null
        };

        if (!temp.HasValue)
            return false;

        arch = temp.Value;
        return true;
    }
}