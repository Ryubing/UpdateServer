using System.Net;
using System.Net.Http.Headers;
using Gommon;

namespace Ryujinx.Systems.Update.Server.Helpers.Http;

public partial class PaginatedEndpoint<T>
{
    private PaginatedEndpoint(IHttpClientProxy client,
        string baseUrl,
        HttpContentParser parsePage)
    {
        _http = client;
        _baseUrl = baseUrl;
        _parsePage = parsePage;
    }

    public async Task<T?> FindOneAsync(Func<T, bool> predicate,
        Action<HttpStatusCode>? onNonSuccess = null)
    {
        var response = await _http.GetAsync(_baseUrl);

        Dictionary<string, string> pageData = ReadPageInformation(response.Headers);

        if (!response.IsSuccessStatusCode)
        {
            onNonSuccess?.Invoke(response.StatusCode);
            return default;
        }

        IEnumerable<T> returned = await _parsePage(response.Content);

        if (returned.TryGetFirst(predicate, out var matched))
            return matched;

        while (pageData.TryGetValue("next", out var link))
        {
            response = await _http.GetAsync(link);

            if (!response.IsSuccessStatusCode)
            {
                onNonSuccess?.Invoke(response.StatusCode);
                return default;
            }

            returned = await _parsePage(response.Content);

            if (returned.TryGetFirst(predicate, out matched))
                return matched;

            pageData = ReadPageInformation(response.Headers);
        }

        return default;
    }

    public async Task<T?> FindOneAsync(Action<HttpStatusCode>? onNonSuccess = null)
    {
        var response = await _http.GetAsync(_baseUrl);

        Dictionary<string, string> pageData = ReadPageInformation(response.Headers);

        if (!response.IsSuccessStatusCode)
        {
            onNonSuccess?.Invoke(response.StatusCode);
            return default;
        }

        var returned = await _parsePage(response.Content).Then(x => x.ToArray());
        if (returned.Length > 0)
            return returned[0];

        while (pageData.TryGetValue("next", out var link))
        {
            response = await _http.GetAsync(link);

            if (!response.IsSuccessStatusCode)
            {
                onNonSuccess?.Invoke(response.StatusCode);
                return default;
            }

            returned = await _parsePage(response.Content).Then(x => x.ToArray());
            if (returned.Length > 0)
                return returned[0];

            pageData = ReadPageInformation(response.Headers);
        }

        return default;
    }

    public async Task<IEnumerable<T>?> GetAllAsync(Func<T, bool> predicate,
        Action<HttpStatusCode>? onNonSuccess = null)
    {
        var response = await _http.GetAsync(_baseUrl);

        Dictionary<string, string> pageData = ReadPageInformation(response.Headers);

        if (!response.IsSuccessStatusCode)
        {
            onNonSuccess?.Invoke(response.StatusCode);
            return null;
        }

        IEnumerable<T> accumulated = await _parsePage(response.Content);

        while (pageData.TryGetValue("next", out var link))
        {
            response = await _http.GetAsync(link);

            if (!response.IsSuccessStatusCode)
            {
                onNonSuccess?.Invoke(response.StatusCode);
                return null;
            }

            accumulated = accumulated.Concat(await _parsePage(response.Content));
            pageData = ReadPageInformation(response.Headers);
        }

        return accumulated.Where(predicate);
    }

    public async Task<IEnumerable<T>?> GetAllAsync(
        Action<HttpStatusCode>? onNonSuccess = null)
    {
        var response = await _http.GetAsync(_baseUrl);

        Dictionary<string, string> pageData = ReadPageInformation(response.Headers);

        if (!response.IsSuccessStatusCode)
        {
            onNonSuccess?.Invoke(response.StatusCode);
            return null;
        }

        IEnumerable<T> accumulated = await _parsePage(response.Content);

        while (pageData.TryGetValue("next", out var link))
        {
            response = await _http.GetAsync(link);

            if (!response.IsSuccessStatusCode)
            {
                onNonSuccess?.Invoke(response.StatusCode);
                return null;
            }

            accumulated = accumulated.Concat(await _parsePage(response.Content));
            pageData = ReadPageInformation(response.Headers);
        }

        return accumulated;
    }

    private Dictionary<string, string> ReadPageInformation(HttpHeaders headers)
    {
        if (!headers.Contains("link")) return new();

        return headers.GetValues("link")
            .SelectMany(x => x.Split(','))
            .Select(extract)
            .ToDictionary();

        KeyValuePair<string, string> extract(string encoded)
        {
            // <https://git.greemdev.net/api/v1/repos/Ryubing/Canary/releases?page=1>; rel="first"

            var keyIdx = encoded.LastIndexOf("rel=", StringComparison.Ordinal);
            var linkEndIdx = encoded.LastIndexOf('>');

            var key = encoded[(keyIdx + 4)..].Trim('"');
            var link = encoded[1..linkEndIdx];
            return new KeyValuePair<string, string>(key, link);
        }
    }
}