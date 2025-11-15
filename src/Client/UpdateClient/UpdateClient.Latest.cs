using System.Net.Http.Json;
using System.Runtime.InteropServices;
using Ryujinx.Systems.Update.Common;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace Ryujinx.Systems.Update.Client;

public partial class UpdateClient
{
    /// <summary>
    ///     Query the latest version of a given release channel matching a specific platform criteria.
    /// </summary>
    /// <param name="platform">The platform to download for.</param>
    /// <param name="arch">The target CPU architecture.</param>
    /// <param name="rc">The desired release channel.</param>
    /// <returns>A <see cref="VersionResponse"/>, or null if any non-200 series HTTP status code is returned from the server.</returns>
    public async Task<VersionResponse?> QueryLatestAsync(
        SupportedPlatform platform,
        SupportedArchitecture arch,
        ReleaseChannel rc = ReleaseChannel.Stable)
    {
        var url = $"{Constants.RouteName_Latest}/{Constants.QueryRoute}" +
                  $"?os={platform.QueryStringValue}" +
                  $"&arch={arch.QueryStringValue}" +
                  $"&rc={rc.QueryStringValue}";

        Log("Checking for updates from: {0}", [QualifyUriPath(url)]);

        var resp = await _http.GetAsync(url);

        if (!resp.IsSuccessStatusCode)
        {
            Log("Received non-success status code ({0}) for update query!",
                [Enum.GetName(resp.StatusCode) ?? $"{(int)resp.StatusCode}"]);
            return null;
        }

        return await resp.Content.ReadFromJsonAsync(JsonSerializerContexts.Default.VersionResponse);
    }

    /// <summary>
    ///     Query the latest version of a given release channel matching the current platform.
    /// </summary>
    /// <param name="rc">The desired release channel.</param>
    /// <returns>A <see cref="VersionResponse"/>, or null if any non-200 series HTTP status code is returned from the server.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the current platform is not some combination of Windows, Linux, or macOS on x86-64 or ARM64.</exception>
    public Task<VersionResponse?> QueryLatestAsync(
        ReleaseChannel rc = ReleaseChannel.Stable) =>
        QueryLatestAsync(
            OperatingSystem.IsWindows()
                ? SupportedPlatform.Windows
                : OperatingSystem.IsLinux()
                    ? SupportedPlatform.Linux
                    : OperatingSystem.IsMacOS()
                        ? SupportedPlatform.Mac
                        : throw new PlatformNotSupportedException(
                            $"The Update Server does not support the current platform: {RuntimeInformation.OSDescription}"),
            RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => SupportedArchitecture.Amd64,
                Architecture.Arm64 => SupportedArchitecture.Arm64,
                _ => throw new PlatformNotSupportedException(
                    $"The Update Server does not support the current platform: {Enum.GetName(RuntimeInformation.ProcessArchitecture) ?? "<unknown CPU arch>"}")
            },
            rc
        );
}