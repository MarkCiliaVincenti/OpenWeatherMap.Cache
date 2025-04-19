using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace OpenWeatherMap.Cache.Services;

internal sealed class DefaultHttpClientFactory(int timeout) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => HttpClientService.CreateHttpClient(timeout);
}

internal class HttpClientService
{
    private readonly HttpClient _httpClient;

    internal HttpClientService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("OpenWeatherMapClient");
    }

    internal Task<HttpResponseMessage> SendAsync(string uri, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead, CancellationToken cancellationToken = default)
    {
        return SendAsync(new Uri(uri), httpCompletionOption, cancellationToken);
    }

    internal async Task<HttpResponseMessage> SendAsync(Uri uri, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead, CancellationToken cancellationToken = default)
    {
        var request = BuildHttpRequestMessage(uri);
        return await _httpClient.SendAsync(request, httpCompletionOption, cancellationToken);
    }

    internal static HttpClient CreateHttpClient(int timeoutMilliseconds)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            CookieContainer = new CookieContainer()
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds)
        };

        return client;
    }

    private static HttpRequestMessage BuildHttpRequestMessage(Uri uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri)
        {
            Version = new Version(1, 1)
        };

        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.TryAddWithoutValidation("If-Modified-Since", DateTime.MinValue.ToString("r"));
        request.Headers.UserAgent.Clear();
        request.Headers.ConnectionClose = true;

        request.Headers.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true
        };

        return request;
    }
}
