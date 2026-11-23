using System.Net;
using System.Net.Http.Json;
using Gommon;
using Ryujinx.Systems.Update.Common;

namespace Ryujinx.Systems.Update.Client;

public partial class UpdateClient
{
    /// <summary>
    ///     Query the sources of the configured version caches on the server. Does not require admin token.
    /// </summary>
    /// <returns>A <see cref="CacheSourceMapping"/> wrapped in a <see cref="Return{T}"/> result, with a <see cref="GenericError{HttpStatusCode}"/> errorState on failure.</returns>
    public async Task<Return<CacheSourceMapping?>> QueryCacheSourcesAsync()
    {
        Log("Checking for cache sources from: {0}", [QualifyUriPath(Constants.FullRouteName_Api_Meta)]);

        var resp = await _http.GetAsync(Constants.FullRouteName_Api_Meta);

        if (!resp.IsSuccessStatusCode)
        {
            Log("Received non-success status code ({0}) for cache sources query!",
                [Enum.GetName(resp.StatusCode) ?? $"{(int)resp.StatusCode}"]);
            return Return<CacheSourceMapping?>.Failure(new GenericError<HttpStatusCode>(resp.StatusCode));
        }

        return await resp.Content.ReadFromJsonAsync(JsonSerializerContexts.Default.CacheSourceMapping);
    }
}