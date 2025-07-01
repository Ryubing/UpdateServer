using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Ryujinx.Systems.Update.Server.Helpers.Http;

namespace Ryujinx.Systems.Update.Server.Services;

public class DefaultHttpClientProxy : IHttpClientProxy, IDisposable
{
    private readonly HttpClient _http;
    private readonly ILogger<IHttpClientProxy>? _logger;

    public DefaultHttpClientProxy(IConfiguration config, ILogger<IHttpClientProxy> logger)
    {
        _logger = logger;

        var gitlabSection = config.GetSection("GitLab");

        if (!gitlabSection.Exists())
            throw new Exception(
                $"The '{gitlabSection.Key}' section does not exist in your appsettings.json. You need to provide an 'Endpoint', 'AccessToken', and optionally 'RefreshIntervalMinutes' values.");

        var host = gitlabSection.GetValue<string>("Endpoint")!.TrimEnd('/');
        var accessToken = gitlabSection.GetValue<string>("AccessToken");

        _http = new HttpClient
        {
            BaseAddress = new Uri(host),
            DefaultRequestHeaders = { Authorization = AuthenticationHeaderValue.Parse($"Bearer {accessToken}") }
        };
    }

    public DefaultHttpClientProxy(HttpClient httpClient, ILogger<IHttpClientProxy>? logger = null)
    {
        _http = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "RedundantAssignment",
        Justification =
            "ReSharper cannot comprehend the idea of checking all combinations of 2 objects potentially being null.")]
    public async Task<HttpResponseMessage> SendAsync(string actualCaller, HttpRequestMessage request,
        HttpCompletionOption? option = null, CancellationToken? token = null)
    {
        HttpResponseMessage response;

        var sw = Stopwatch.StartNew();

        if (option is null && token is not null)
            response = await _http.SendAsync(request, token.Value);
        if (option is not null && token is null)
            response = await _http.SendAsync(request, option.Value);
        if (option is not null && token is not null)
            response = await _http.SendAsync(request, option.Value, token.Value);
        else
            response = await _http.SendAsync(request);

        sw.Stop();


        _logger?.LogInformation(
            new EventId(1, actualCaller),
            "{method} {uri} -> {statusCode} in {elapsed}ms",
            request.Method.Method,
            request.RequestUri!.ToString(),
            (int)response.StatusCode,
            sw.Elapsed.TotalMilliseconds
        );

        return response;
    }

    public void Dispose() => _http.Dispose();
}