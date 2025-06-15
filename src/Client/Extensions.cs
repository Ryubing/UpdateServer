namespace Ryujinx.Systems.Update.Client;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

public static class Extensions
{
    /// <summary>
    ///     Sets the position in the current <see cref="Stream"/> to 0 and write the entire contents of it to the file specified at the path.
    /// </summary>
    /// <param name="stream">The stream to copy data from.</param>
    /// <param name="filePath">The target file path to copy the stream's contents. Will be created if it doesn't exist.</param>
    public static async Task WriteToFileAsync(this Stream stream, string filePath)
    {
        stream.Seek(0, SeekOrigin.Begin);
        await using var f = File.OpenWrite(filePath);
        await stream.CopyToAsync(f);
    }
}