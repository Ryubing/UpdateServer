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
            => WithContentParser(content => content.ReadFromJsonAsync(typeInfo)!);

        public PaginatedEndpoint<T> Build() => new(_http, BaseUrl, ContentParser);

        public static implicit operator PaginatedEndpoint<T>(BuilderApi builder) => builder.Build();
    }
}