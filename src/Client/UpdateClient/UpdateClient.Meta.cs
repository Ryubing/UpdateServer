using System.Net.Http.Json;
using Ryujinx.Systems.Update.Common;

namespace Ryujinx.Systems.Update.Client;

public partial class UpdateClient
{
    /// <summary>
    ///     Query the sources of the configured version caches on the server. Does not require admin token.
    /// </summary>
    /// <returns>A <see cref="CacheSourceMapping"/>, or null if any non-200 series HTTP status code is returned from the server.</returns>
    public async Task<CacheSourceMapping?> QueryCacheSourcesAsync()
    {
        Log("Checking for cache sources from: {0}", [QualifyUriPath(Constants.FullRouteName_Api_Meta)]);

        var resp = await _http.GetAsync(Constants.FullRouteName_Api_Meta);

        if (!resp.IsSuccessStatusCode)
        {
            Log("Received non-success status code ({0}) for cache sources query!",
                [Enum.GetName(resp.StatusCode) ?? $"{(int)resp.StatusCode}"]);
            return null;
        }

        return await resp.Content.ReadFromJsonAsync(JsonSerializerContexts.Default.CacheSourceMapping);
    }
}