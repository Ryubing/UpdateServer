using System.Net;
using System.Text.Json.Serialization.Metadata;

namespace Ryujinx.Systems.Update.Server.Services.GitLab;

public static class HttpHelpers
{
    public static Func<HttpContent, Task<T>> ReadResonseAs<T>(JsonTypeInfo<T> typeInfo) 
        => async content => (await content.ReadFromJsonAsync(typeInfo))!;

    public static Task<IEnumerable<T>?> PaginateAsync<T>(
        this HttpClient http,
        string endpoint,
        JsonTypeInfo<IEnumerable<T>> typeInfo,
        Action<HttpStatusCode>? onNonSuccess = null)
        => http.PaginateAsync(endpoint, ReadResonseAs(typeInfo), onNonSuccess);
    public static async Task<IEnumerable<T>?> PaginateAsync<T>(
        this HttpClient http,
        string endpoint,
        Func<HttpContent, Task<IEnumerable<T>>> converter,
        Action<HttpStatusCode>? onNonSuccess = null)
    {
        var response = await http.GetAsync(endpoint);

        if (!response.IsSuccessStatusCode)
        {
            onNonSuccess?.Invoke(response.StatusCode);
            return null;
        }

        IEnumerable<T> accumulated = await converter(response.Content);

        if (!response.Headers.GetValues("x-total-pages").ToString().TryParse<int>(out var pageCount) || pageCount > 1)
        {
            var currentPage = 2;
            do
            {
                var pageResponse = await http.GetAsync($"{endpoint}&page={currentPage}");

                if (!pageResponse.IsSuccessStatusCode)
                {
                    onNonSuccess?.Invoke(pageResponse.StatusCode);
                    return null;
                }

                accumulated = accumulated.Concat(await converter(pageResponse.Content));

                currentPage++;
            } while (currentPage <= pageCount);
        }

        return accumulated;
    }
}