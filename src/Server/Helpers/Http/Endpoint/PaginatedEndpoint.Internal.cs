using System.Text;

namespace Ryujinx.Systems.Update.Server.Helpers.Http;

public partial class PaginatedEndpoint<T>
{
    private readonly IHttpClientProxy _http;
    private readonly string _baseUrl;
    private readonly HttpContentParser _parsePage;

    public delegate Task<IEnumerable<T>> HttpContentParser(HttpContent content);
}