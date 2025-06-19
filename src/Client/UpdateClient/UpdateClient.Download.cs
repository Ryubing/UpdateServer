using Ryujinx.Systems.Update.Common;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace Ryujinx.Systems.Update.Client;

public partial class UpdateClient
{
    /// <summary>
    ///     Directly downloads a file from the update server matching the query parameters.
    /// </summary>
    /// <param name="version">The version to download.</param>
    /// <param name="platform">The platform to download for.</param>
    /// <param name="arch">The target CPU architecture.</param>
    /// <param name="rc">The desired release channel.</param>
    /// <returns>The HTTP response content body as a <see cref="Stream"/>, or null if any non-200 series HTTP status code is returned from the server.</returns>
    public async Task<Stream?> DownloadAsync(
        string version,    
        SupportedPlatform platform,
        SupportedArchitecture arch,
        ReleaseChannel rc = ReleaseChannel.Stable)
    {
        var url = $"{Constants.RouteName_Download}/{Constants.QueryRoute}" +
                  $"?os={platform.AsQueryStringValue()}" +
                  $"&arch={arch.AsQueryStringValue()}" +
                  $"&rc={rc.AsQueryStringValue()}" +
                  $"&version={version}";
        
        Log("Downloading the file from: {0}", [QualifyUriPath(url)]);
        
        var resp = await _http.GetAsync(url);

        if (!resp.IsSuccessStatusCode)
        {
            Log("Received non-success status code ({0}) for download query!", [Enum.GetName(resp.StatusCode) ?? $"{(int)resp.StatusCode}"]);
            return null;
        }

        return await resp.Content.ReadAsStreamAsync();
    }

    /// <summary>
    ///     Directly downloads the latest update from the update server matching the query parameters.
    /// </summary>
    /// <param name="platform">The platform to download for.</param>
    /// <param name="arch">The target CPU architecture.</param>
    /// <param name="rc">The desired release channel.</param>
    /// <returns>The HTTP response content body as a <see cref="Stream"/>, or null if any non-200 series HTTP status code is returned from the server.</returns>
    public Task<Stream?> DownloadLatestAsync(
        SupportedPlatform platform,
        SupportedArchitecture arch,
        ReleaseChannel rc = ReleaseChannel.Stable) => DownloadAsync("latest", platform, arch, rc);
}