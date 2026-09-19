// Copyright (c) All contributors.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

internal sealed class HttpClientService : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposedValue;

    internal HttpClientService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("OpenWeatherMapClient");
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true
        };
    }

    internal Task<HttpResponseMessage> SendAsync(string uri, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead, CancellationToken cancellationToken = default)
    {
        return _httpClient.GetAsync(uri, httpCompletionOption, cancellationToken);
    }

    internal Task<HttpResponseMessage> SendAsync(Uri uri, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead, CancellationToken cancellationToken = default)
    {
        return _httpClient.GetAsync(uri, httpCompletionOption, cancellationToken);
    }

    internal static HttpClient CreateHttpClient(int timeoutMilliseconds)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
#pragma warning restore CA2000 // Dispose objects before losing scope

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds)
        };

        return client;
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
