using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.Systems.Update.Server;

public static class Extensions
{
    public static bool TryParse<T>(this string? s, [MaybeNullWhen(false)] out T result, IFormatProvider? formatProvider = null) where T : IParsable<T>
        => T.TryParse(s, formatProvider, out result);
    
    public static T Parse<T>(this string s, IFormatProvider? formatProvider = null) where T : IParsable<T>
        => T.Parse(s, formatProvider);
    
    public static bool TryParse<T>(this ReadOnlySpan<char> s, [MaybeNullWhen(false)] out T result, IFormatProvider? formatProvider = null) where T : ISpanParsable<T>
        => T.TryParse(s, formatProvider, out result);
    
    public static T Parse<T>(this ReadOnlySpan<char> s, IFormatProvider? formatProvider = null) where T : ISpanParsable<T>
        => T.Parse(s, formatProvider);
}