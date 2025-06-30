using System.Net;

namespace Ryujinx.Systems.Update.Server.Helpers.Http;

public partial class PaginatedEndpoint<T>
{
    private PaginatedEndpoint(IHttpClientProxy client, 
        string baseUrl, 
        HttpContentParser parsePage, 
        Dictionary<string, object> queryStringParams, 
        int perPage = 100)
    {
        _http = client;
        _baseUrl = baseUrl;
        _parsePage = parsePage;
        _queryStringParams = queryStringParams;
        _queryStringParams["per_page"] = perPage;
    }
    
    public async Task<T?> FindOneAsync(Func<T, bool> predicate,
        Action<HttpStatusCode>? onNonSuccess = null)
    {
        var currentPage = 1;
        var response = await _http.GetAsync(GetUrl(currentPage));

        if (!response.IsSuccessStatusCode)
        {
            onNonSuccess?.Invoke(response.StatusCode);
            return default;
        }

        IEnumerable<T> returned = await _parsePage(response.Content);

        if (returned.TryGetFirst(predicate, out var matched))
            return matched;
        
        if (!response.Headers.GetValues("x-total-pages").ToString().TryParse<int>(out var pageCount) || pageCount > 1)
        {
            currentPage++;
            do
            {
                response = await _http.GetAsync(GetUrl(currentPage));

                if (!response.IsSuccessStatusCode)
                {
                    onNonSuccess?.Invoke(response.StatusCode);
                    return default;
                }

                returned = await _parsePage(response.Content);
                
                if (returned.TryGetFirst(predicate, out matched))
                    return matched;

                currentPage++;
            } while (currentPage <= pageCount);
        }

        return default;
    }
    
    public async Task<T?> FindOneAsync(Action<HttpStatusCode>? onNonSuccess = null)
    {
        var currentPage = 1;
        var response = await _http.GetAsync(GetUrl(currentPage));

        if (!response.IsSuccessStatusCode)
        {
            onNonSuccess?.Invoke(response.StatusCode);
            return default;
        }

        var returned = (await _parsePage(response.Content)).ToArray();
        if (returned.Length > 0)
            return returned[0];
        
        if (!response.Headers.GetValues("x-total-pages").ToString().TryParse<int>(out var pageCount) || pageCount > 1)
        {
            currentPage++;
            do
            {
                response = await _http.GetAsync(GetUrl(currentPage));

                if (!response.IsSuccessStatusCode)
                {
                    onNonSuccess?.Invoke(response.StatusCode);
                    return default;
                }

                returned = (await _parsePage(response.Content)).ToArray();
                if (returned.Length > 0)
                    return returned[0];

                currentPage++;
            } while (currentPage <= pageCount);
        }

        return default;
    }
    
    public async Task<IEnumerable<T>?> GetAllAsync(Func<T, bool> predicate,
        Action<HttpStatusCode>? onNonSuccess = null)
    {
        var currentPage = 1;
        var response = await _http.GetAsync(GetUrl(currentPage));

        if (!response.IsSuccessStatusCode)
        {
            onNonSuccess?.Invoke(response.StatusCode);
            return null;
        }

        IEnumerable<T> accumulated = await _parsePage(response.Content);

        if (!response.Headers.GetValues("x-total-pages").ToString().TryParse<int>(out var pageCount) || pageCount > 1)
        {
            currentPage++;
            do
            {
                response = await _http.GetAsync(GetUrl(currentPage));

                if (!response.IsSuccessStatusCode)
                {
                    onNonSuccess?.Invoke(response.StatusCode);
                    return null;
                }

                accumulated = accumulated.Concat(await _parsePage(response.Content));

                currentPage++;
            } while (currentPage <= pageCount);
        }

        return accumulated.Where(predicate);
    }
    
    public async Task<IEnumerable<T>?> GetAllAsync(
        Action<HttpStatusCode>? onNonSuccess = null)
    {
        var currentPage = 1;
        var response = await _http.GetAsync(GetUrl(currentPage));

        if (!response.IsSuccessStatusCode)
        {
            onNonSuccess?.Invoke(response.StatusCode);
            return null;
        }

        IEnumerable<T> accumulated = await _parsePage(response.Content);

        if (!response.Headers.GetValues("x-total-pages").ToString().TryParse<int>(out var pageCount) || pageCount > 1)
        {
            currentPage++;
            do
            {
                response = await _http.GetAsync(GetUrl(currentPage));

                if (!response.IsSuccessStatusCode)
                {
                    onNonSuccess?.Invoke(response.StatusCode);
                    return null;
                }

                accumulated = accumulated.Concat(await _parsePage(response.Content));

                currentPage++;
            } while (currentPage <= pageCount);
        }

        return accumulated;
    }
}