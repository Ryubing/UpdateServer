using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Ryujinx.Systems.Update.Server.Helpers.Http;

namespace Ryujinx.Systems.Update.Server.Services;

public class DefaultHttpClientProxy : IHttpClientProxy, IDisposable
{
    public delegate void LogCallback(string fmt, object[] fmtArgs, string caller);
    
    private readonly HttpClient _http;
    private readonly LogCallback? _callback;

    public DefaultHttpClientProxy(IConfiguration config, ILogger<IHttpClientProxy> logger)
    {
        var gitlabSection = config.GetSection("GitLab");
        
        if (!gitlabSection.Exists())
            throw new Exception($"The '{gitlabSection.Key}' section does not exist in your appsettings.json. You need to provide an 'Endpoint', 'AccessToken', and optionally 'RefreshIntervalMinutes' values.");

        var host = gitlabSection.GetValue<string>("Endpoint")!.TrimEnd('/');
        var accessToken = gitlabSection.GetValue<string>("AccessToken");
        
        _http = new HttpClient
        {
            BaseAddress = new Uri(host),
            DefaultRequestHeaders = { Authorization = AuthenticationHeaderValue.Parse($"Bearer {accessToken}") }
        };
        
        _callback = (format, formatArgs, _) =>
        {
#pragma warning disable CA2254
            logger.LogInformation(format, formatArgs);
#pragma warning restore CA2254
        };
    }
    
    public DefaultHttpClientProxy(HttpClient httpClient, LogCallback? logCallback = null)
    {
        _http = httpClient;
        _callback = logCallback;
    }

    [SuppressMessage("ReSharper", "RedundantAssignment", Justification = "ReSharper cannot comprehend the idea of checking all combinations of 2 objects potentially being null.")]
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption? option = null, CancellationToken? token = null)
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
        
        Log("{method} {uri} -> {statusCode} in {elapsed}ms", GetLogArgs(request, response, sw));

        return response;
    }
    
    private void Log(string messageFormat, object[]? formatArgs = null, [CallerMemberName] string caller = null!) 
        => _callback?.Invoke(messageFormat, formatArgs ?? [], caller);

    private object[] GetLogArgs(HttpRequestMessage request, HttpResponseMessage response, Stopwatch sw)
    {
        var result = new object[4];
        result[0] = request.Method.Method;
        result[1] = request.RequestUri!.ToString();
        result[2] = (int)response.StatusCode;
        result[3] = sw.Elapsed.TotalMilliseconds;
        return result;
    }

    public void Dispose() => _http.Dispose();
}
