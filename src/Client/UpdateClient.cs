using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Updater.Common;

namespace Ryujinx.Systems.Update.Client;

public class UpdateClient : IDisposable
{
    private readonly UpdateClientConfig _config;
    private readonly HttpClient _http;
    
    public UpdateClient(UpdateClientConfig config)
    {
        _config = config;
        _http = new HttpClient
        {
            BaseAddress = new Uri(_config.ServerEndpoint ??
                                  throw new NullReferenceException(
                                      "Cannot create an UpdateClient with no server endpoint."))
        };
    }
    
    public async Task<VersionResponse?> QueryLatestAsync(
        SupportedPlatform platform,
        SupportedArchitecture arch,
        ReleaseChannel rc = ReleaseChannel.Stable)
    {
        var resp = await _http.GetAsync($"{Constants.RouteName_Latest}/{Constants.QueryRoute}" +
                                        $"?os={platform.AsQueryStringValue()}" +
                                        $"&arch={arch.AsQueryStringValue()}" +
                                        $"&rc={rc.AsQueryStringValue()}");

        if (!resp.IsSuccessStatusCode)
            return null;

        return await resp.Content.ReadFromJsonAsync(JsonSerializerContexts.Default.VersionResponse);
    }
    
    /// <exception cref="PlatformNotSupportedException">Thrown when the current platform is not some combination of Windows, Linux, or macOS on x86-64 or ARM64.</exception>
    public Task<VersionResponse?> QueryLatestAsync(
        ReleaseChannel rc = ReleaseChannel.Stable)
    {
        SupportedPlatform platform;
        SupportedArchitecture arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => SupportedArchitecture.Amd64,
            Architecture.Arm64 => SupportedArchitecture.Arm64,
            _ => throw new PlatformNotSupportedException($"The Update Server does not support the current platform: {Enum.GetName(RuntimeInformation.ProcessArchitecture) ?? "<unknown CPU arch>"}")
        };

        if (OperatingSystem.IsWindows()) platform = SupportedPlatform.Windows;
        else if (OperatingSystem.IsLinux()) platform = SupportedPlatform.Linux;
        else if (OperatingSystem.IsMacOS()) platform = SupportedPlatform.Mac;
        else throw new PlatformNotSupportedException($"The Update Server does not support the current platform: {RuntimeInformation.OSDescription}");

        return QueryLatestAsync(platform, arch, rc);
    }
    
    public async Task<Stream?> DownloadAsync(
        string version,    
        SupportedPlatform platform,
        SupportedArchitecture arch,
        ReleaseChannel rc = ReleaseChannel.Stable)
    {
        var resp = await _http.GetAsync($"{Constants.RouteName_Download}/{Constants.QueryRoute}" +
                                        $"?os={platform.AsQueryStringValue()}" +
                                        $"&arch={arch.AsQueryStringValue()}" +
                                        $"&rc={rc.AsQueryStringValue()}" +
                                        $"&version={version}");

        if (!resp.IsSuccessStatusCode)
            return null;

        return await resp.Content.ReadAsStreamAsync();
    }

    public Task<Stream?> DownloadLatestAsync(
        SupportedPlatform platform,
        SupportedArchitecture arch,
        ReleaseChannel rc = ReleaseChannel.Stable) => DownloadAsync("latest", platform, arch, rc);

    public async Task<bool?> RefreshVersionCacheAsync(ReleaseChannel rc)
    {
        if (!_config.CanUseAdminEndpoints)
        {
            _config.Logger("Cannot use {0} endpoint, as there is no configured admin access token.", [nameof(RefreshVersionCacheAsync)]);
            return null;
        }
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Patch, $"{Constants.FullRouteName_Api_Admin_RefreshCache}?rc={rc.AsQueryStringValue()}");
        ApplyAuthorization(httpRequest);

        var response = await _http.SendAsync(httpRequest);
        if (!response.IsSuccessStatusCode)
        {
            _config.Logger("{0} endpoint failed: received status code {1}; content body: {2}",
                [nameof(RefreshVersionCacheAsync), (int)response.StatusCode, await response.Content.ReadAsStringAsync()]);
            return false;
        }

        return true;
    }
    
    public void Dispose() => _http.Dispose();
    
    private void ApplyAuthorization(HttpRequestMessage httpRequest)
    {
        httpRequest.Headers.Add("Authorization", _config.AdminAccessToken);
    }
}