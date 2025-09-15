using System.Text.Json.Serialization.Metadata;
using Gommon;
using Ryujinx.Systems.Update.Server.Services;

namespace Ryujinx.Systems.Update.Server.Helpers.Http;

public partial class PaginatedEndpoint<T>
{
    public static BuilderApi Builder(IHttpClientProxy httpClient) => new(httpClient);
    
    public static BuilderApi Builder(HttpClient httpClient) => new(new DefaultHttpClientProxy(httpClient));
    
    public class BuilderApi
    {
        public BuilderApi(IHttpClientProxy httpClient)
        {
            _http = httpClient;
        }

        private readonly IHttpClientProxy _http;
        
        public string BaseUrl { get; private set; } = null!;
        public HttpContentParser ContentParser { get; private set; } = null!;
        public int PerPage { get; private set; } = 100;

        public Dictionary<string, object> QueryStringParameters { get; private set; } = new();

        public BuilderApi WithBaseUrl(string url)
        {
            BaseUrl = url;
            return this;
        }
        
        public BuilderApi WithContentParser(HttpContentParser contentParser)
        {
            ContentParser = contentParser;
            return this;
        }
        
        public BuilderApi WithJsonContentParser(JsonTypeInfo<IEnumerable<T>> typeInfo)
        {
            ContentParser = content => content.ReadFromJsonAsync(typeInfo)!;
            return this;
        }
        
        public BuilderApi WithPerPageCount(int perPage)
        {
            PerPage = perPage;
            return this;
        }

        public BuilderApi WithQueryStringParameters(params (string, object)[] parameters)
        {
            QueryStringParameters = Collections.NewSafeDictionary(parameters);
            return this;
        }

        public PaginatedEndpoint<T> Build() => new(_http, BaseUrl, ContentParser, QueryStringParameters, PerPage);

        public static implicit operator PaginatedEndpoint<T>(BuilderApi builder) => builder.Build();
    }
}