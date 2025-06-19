using Ryujinx.Systems.Update.Common;
using Ryujinx.Systems.Updater.Common;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace Ryujinx.Systems.Update.Client;

public partial class UpdateClient
{
    /// <summary>
    ///     Requests the configured update server to refresh its version cache for a given release channel.
    /// </summary>
    /// <param name="rc">The target release channel.</param>
    /// <returns>true if request success; false if non-200 series HTTP status code, null if not configured to support this endpoint.</returns>
    /// <remarks>Requires an authorization-bearing client configuration.</remarks>
    public async Task<bool?> RefreshVersionCacheAsync(ReleaseChannel rc)
    {
        if (!_config.CanUseAdminEndpoints)
        {
            Log("Cannot request cache refresh, as there is no configured admin access token.");
            return null;
        }
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Patch, $"{Constants.FullRouteName_Api_Admin_RefreshCache}?rc={rc.AsQueryStringValue()}");
        ApplyAuthorization(httpRequest);
        
        if (await _http.SendAsync(httpRequest) is { IsSuccessStatusCode: false} resp)
        {
            Log("Refreshing version cache failed: received status code {0}; content body: {1}",
                [Enum.GetName(resp.StatusCode) ?? $"{(int)resp.StatusCode}", await resp.Content.ReadAsStringAsync()]);
            return false;
        }

        return true;
    }
}