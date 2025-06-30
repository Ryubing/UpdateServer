using System.Text;

namespace Ryujinx.Systems.Update.Server.Helpers.Http;

public partial class PaginatedEndpoint<T>
{
    private readonly IHttpClientProxy _http;
    private readonly string _baseUrl;
    private readonly HttpContentParser _parsePage;
    private readonly Dictionary<string, object> _queryStringParams;
    private string? _constructedUrl;

    private string GetUrl(int pageNumber)
    {
        if (_constructedUrl is null)
        {
            var sb = new StringBuilder(_baseUrl.TrimEnd('/'));
            foreach (var (index, (param, value)) in _queryStringParams.Index())
            {
                sb.Append(index is 0 ? "?" : "&");

                sb.Append(param).Append('=').Append(value);
            }

            _constructedUrl = sb.ToString();
        }

        return $"{_constructedUrl}&page={pageNumber}";
    }

    public delegate Task<IEnumerable<T>> HttpContentParser(HttpContent content);
}