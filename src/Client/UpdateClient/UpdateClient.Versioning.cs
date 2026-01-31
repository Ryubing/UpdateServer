using Ryujinx.Systems.Update.Common;

namespace Ryujinx.Systems.Update.Client;

public partial class UpdateClient
{
    /// <summary>
    ///     Query the next version for a release channel.
    /// </summary>
    /// <param name="rc">The target release channel.</param>
    /// <param name="isMajorRelease">If the next version should be a major release, incrementing the second number instead of third. Mainly used in CI.</param>
    /// <returns>Plain version string if request success; null if non-200 series HTTP status code or if not configured to support this endpoint.</returns>
    public async Task<string?> GetNextVersionAsync(ReleaseChannel rc, bool isMajorRelease = false)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Get,
            $"{Constants.FullRouteName_Api_Versioning}/{Constants.RouteName_Api_Versioning_GetNextVersion}?rc={rc.QueryStringValue}&major={isMajorRelease.ToString().ToLower()}");

        var resp = await _http.SendAsync(httpRequest);

        if (!resp.IsSuccessStatusCode)
        {
            Log("Getting next version failed: received status code {0}; content body: {1}",
                [Enum.GetName(resp.StatusCode) ?? $"{(int)resp.StatusCode}", await resp.Content.ReadAsStringAsync()]);
            return null;
        }

        return await resp.Content.ReadAsStringAsync();
    }
    
    /// <summary>
    ///     Query the current version for a release channel.
    /// </summary>
    /// <param name="rc">The target release channel.</param>
    /// <returns>Plain version string if request success; null if non-200 series HTTP status code or if not configured to support this endpoint.</returns>
    public async Task<string?> GetCurrentVersionAsync(ReleaseChannel rc)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Get,
            $"{Constants.FullRouteName_Api_Versioning}/{Constants.RouteName_Api_Versioning_GetCurrentVersion}?rc={rc.QueryStringValue}");

        var resp = await _http.SendAsync(httpRequest);

        if (!resp.IsSuccessStatusCode)
        {
            Log("Getting current version failed: received status code {0}; content body: {1}",
                [Enum.GetName(resp.StatusCode) ?? $"{(int)resp.StatusCode}", await resp.Content.ReadAsStringAsync()]);
            return null;
        }

        return await resp.Content.ReadAsStringAsync();
    }

    /// <summary>
    ///     Requests the configured update server to increment its version for a given release channel.
    /// </summary>
    /// <param name="rc">The target release channel.</param>
    /// <returns>true if request success; false if non-200 series HTTP status code, null if not configured to support this endpoint.</returns>
    /// <remarks>Requires an authorization-bearing client configuration.</remarks>
    public async Task<bool?> IncrementVersionAsync(ReleaseChannel rc)
    {
        if (!_config.CanUseAdminEndpoints)
        {
            Log("Cannot request version increment, as there is no configured admin access token.");
            return null;
        }

        var httpRequest = new HttpRequestMessage(HttpMethod.Patch,
            $"{Constants.FullRouteName_Api_Versioning}/{Constants.RouteName_Api_Versioning_IncrementVersion}?rc={rc.QueryStringValue}");
        ApplyAuthorization(httpRequest);

        if (await _http.SendAsync(httpRequest) is { IsSuccessStatusCode: false } resp)
        {
            Log("Increment version failed: received status code {0}; content body: {1}",
                [Enum.GetName(resp.StatusCode) ?? $"{(int)resp.StatusCode}", await resp.Content.ReadAsStringAsync()]);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Requests the configured update server to increment the major version by one for both stable and canary, and to reset the build number to 0.
    /// </summary>
    /// <param name="rc">The target release channel.</param>
    /// <returns>true if request success; false if non-200 series HTTP status code, null if not configured to support this endpoint.</returns>
    /// <remarks>Requires an authorization-bearing client configuration.</remarks>
    public async Task<bool?> AdvanceVersionAsync(ReleaseChannel? rc = null)
    {
        if (!_config.CanUseAdminEndpoints)
        {
            Log("Cannot request version advance, as there is no configured admin access token.");
            return null;
        }

        var route = $"{Constants.FullRouteName_Api_Versioning}/{Constants.RouteName_Api_Versioning_AdvanceVersion}";
        if (rc != null)
            route += $"?rc={rc.Value.QueryStringValue}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Patch, route);
        ApplyAuthorization(httpRequest);

        if (await _http.SendAsync(httpRequest) is { IsSuccessStatusCode: false } resp)
        {
            Log("Advancing version failed: received status code {0}; content body: {1}",
                [Enum.GetName(resp.StatusCode) ?? $"{(int)resp.StatusCode}", await resp.Content.ReadAsStringAsync()]);
            return false;
        }

        return true;
    }
}