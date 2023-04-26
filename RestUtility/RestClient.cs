using System.Net;
using System.Net.Mime;
using System.Text;
using RestUtility.Authentication;
using RestUtility.Handlers;
using RestUtility.Serializers;

namespace RestUtility;

/// <summary>
/// REST API client
/// </summary>
public sealed class RestClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly HttpClientHandler _httpClientHandler;

    private readonly AuthenticationPipeline? _authenticationPipeline;
    private readonly SerializerPipeline? _serializerPipeline;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public CookieContainer Cookies { get; } = new();

    public bool UseCookies
    {
        get => _httpClientHandler.UseCookies;
        set => _httpClientHandler.UseCookies = value;
    }

    public bool AllowAutomaticRedirect
    {
        get => _httpClientHandler.AllowAutoRedirect;
        set => _httpClientHandler.AllowAutoRedirect = value;
    }

    public int MaxAutomaticRedirections
    {
        get => _httpClientHandler.MaxAutomaticRedirections;
        set => _httpClientHandler.MaxAutomaticRedirections = value;
    }

    /// <inheritdoc />
    public RestClient()
        : this(Array.Empty<DelegatingHandler>()) { }

    /// <inheritdoc />
    public RestClient(AuthenticationPipeline authenticationPipeline)
        : this(Array.Empty<DelegatingHandler>(), authenticationPipeline, null) { }

    /// <inheritdoc />
    public RestClient(SerializerPipeline serializerPipelinePipeline)
        : this(Array.Empty<DelegatingHandler>(), null, serializerPipelinePipeline) { }

    /// <inheritdoc />
    public RestClient(IEnumerable<DelegatingHandler> delegatingHandlers, AuthenticationPipeline authenticationPipeline)
        : this(delegatingHandlers, authenticationPipeline, null) { }

    /// <inheritdoc />
    public RestClient(IEnumerable<DelegatingHandler> delegatingHandlers, SerializerPipeline serializerPipelinePipeline)
        : this(delegatingHandlers, null, serializerPipelinePipeline) { }

    /// <summary>
    /// Instantiates a new instance of the REST API client.
    /// </summary>
    /// <param name="delegatingHandlers">HTTP(S) pipelining delegating handlers</param>
    /// <param name="authenticationPipeline">Authentication pipeline</param>
    /// <param name="serializerPipelinePipeline">Serialization pipeline</param>
    public RestClient(
        IEnumerable<DelegatingHandler> delegatingHandlers,
        AuthenticationPipeline? authenticationPipeline = null,
        SerializerPipeline? serializerPipelinePipeline = null)
    {
        var handlers = new List<DelegatingHandler>(delegatingHandlers) {
            new TimeoutHandler(),  // Allows for a timeout to be specified per request
            new PipelineHandler(), // Automates the authentication and serialization pipelines
        };

        _authenticationPipeline = authenticationPipeline;
        _serializerPipeline = serializerPipelinePipeline;
        _httpClientHandler = new HttpClientHandler { /* MaxConnectionsPerServer = 256, */ CookieContainer = Cookies };
        _httpClient = CreateHttpClientWithHandlers(_httpClientHandler, handlers);
    }

    private static HttpClient CreateHttpClientWithHandlers(
        HttpMessageHandler messageHandler,
        IEnumerable<DelegatingHandler> delegatingHandlers)
    {
        if (messageHandler is null)
            throw new ArgumentNullException(nameof(messageHandler));

        HttpMessageHandler httpMessageHandler = messageHandler;
        foreach (DelegatingHandler? delegatingHandler in delegatingHandlers.Reverse())
        {
            if (delegatingHandler is null)
                throw new ArgumentException("DelegatingHandler is null");

            if (delegatingHandler.InnerHandler is not null)
                throw new ArgumentException("Delegating inner handler already assigned");

            delegatingHandler.InnerHandler = httpMessageHandler;
            httpMessageHandler = delegatingHandler;
        }

        return new HttpClient(httpMessageHandler)
        {
            Timeout = Timeout.InfiniteTimeSpan, // Disable the client timeout since we use a timeout per request
            DefaultRequestHeaders = { ConnectionClose = true },
        };
    }

    #region [ GET Requests ]
    /// <summary>
    /// Sends a GET request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> GetAsync(string requestUri)
    {
        return await GetAsync(new Uri(requestUri)).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a GET request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> GetAsync(Uri requestUri)
    {
        return await GetAsync(requestUri, Timeout.InfiniteTimeSpan).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a GET request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> GetAsync(string requestUri, TimeSpan requestTimeout)
    {
        return await GetAsync(new Uri(requestUri), requestTimeout).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a GET request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> GetAsync(Uri requestUri, TimeSpan requestTimeout)
    {
        return await SendRequestAsync<object>(HttpMethod.Get, requestUri, null, requestTimeout, false).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a GET request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<T>> GetAsync<T>(string requestUri)
        where T : class
    {
        return await GetAsync<T>(new Uri(requestUri)).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a GET request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<T>> GetAsync<T>(Uri requestUri)
        where T : class
    {
        return await GetAsync<T>(requestUri, Timeout.InfiniteTimeSpan).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a GET request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<T>> GetAsync<T>(string requestUri, TimeSpan requestTimeout)
        where T : class
    {
        return await GetAsync<T>(new Uri(requestUri), requestTimeout).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a GET request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<T>> GetAsync<T>(Uri requestUri, TimeSpan requestTimeout)
        where T : class
    {
        return await SendRequestAsync<T>(HttpMethod.Get, requestUri, null, requestTimeout, true).ConfigureAwait(false);
    }
    #endregion

    #region [ Post Requests ]
    /// <summary>
    /// Sends a POST request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> PostAsync(
        string requestUri, string requestBody,
        string contentType = MediaTypeNames.Text.Plain)
    {
        return await PostAsync(new Uri(requestUri), requestBody, Timeout.InfiniteTimeSpan, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a POST request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> PostAsync(
        Uri requestUri, string requestBody,
        string contentType = MediaTypeNames.Text.Plain)
    {
        return await PostAsync(requestUri, requestBody, Timeout.InfiniteTimeSpan, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a POST request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> PostAsync(
        string requestUri, string requestBody, TimeSpan requestTimeout,
        string contentType = MediaTypeNames.Text.Plain)
    {
        return await PostAsync(new Uri(requestUri), requestBody, requestTimeout, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a POST request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> PostAsync(
        Uri requestUri, string requestBody, TimeSpan requestTimeout,
        string contentType = MediaTypeNames.Text.Plain)
    {
        using var content = new StringContent(requestBody, Encoding.UTF8, contentType);
        return await SendRequestAsync<object>(HttpMethod.Post, requestUri, content, requestTimeout, false).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a POST request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<T>> PostAsync<T>(
        Uri requestUri, string requestBody,
        string contentType = MediaTypeNames.Text.Plain)
        where T : class
    {
        return await PostAsync<T>(requestUri, requestBody, Timeout.InfiniteTimeSpan, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a POST request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<T>> PostAsync<T>(
        string requestUri, string requestBody, TimeSpan requestTimeout,
        string contentType = MediaTypeNames.Text.Plain)
        where T : class
    {
        return await PostAsync<T>(new Uri(requestUri), requestBody, requestTimeout, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a POST request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<T>> PostAsync<T>(
        Uri requestUri, string requestBody, TimeSpan requestTimeout,
        string contentType = MediaTypeNames.Text.Plain)
        where T : class
    {
        using var content = new StringContent(requestBody, Encoding.UTF8, contentType);
        return await SendRequestAsync<T>(HttpMethod.Post, requestUri, content, requestTimeout, true).ConfigureAwait(false);
    }
    #endregion

    #region [ Put Requests ]
    /// <summary>
    /// Sends a PUT request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> PutAsync(
        string requestUri, string requestBody,
        string contentType = MediaTypeNames.Text.Plain)
    {
        return await PutAsync(new Uri(requestUri), requestBody, Timeout.InfiniteTimeSpan, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a PUT request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> PutAsync(
        Uri requestUri, string requestBody,
        string contentType = MediaTypeNames.Text.Plain)
    {
        return await PutAsync(requestUri, requestBody, Timeout.InfiniteTimeSpan, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a PUT request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> PutAsync(
        string requestUri, string requestBody, TimeSpan requestTimeout,
        string contentType = MediaTypeNames.Text.Plain)
    {
        return await PutAsync(new Uri(requestUri), requestBody, requestTimeout, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a PUT request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> PutAsync(
        Uri requestUri, string requestBody, TimeSpan requestTimeout,
        string contentType = MediaTypeNames.Text.Plain)
    {
        using var content = new StringContent(requestBody, Encoding.UTF8, contentType);
        return await SendRequestAsync<object>(HttpMethod.Put, requestUri, content, requestTimeout, false).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a PUT request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<T>> PutAsync<T>(
        Uri requestUri, string requestBody,
        string contentType = MediaTypeNames.Text.Plain)
        where T : class
    {
        return await PutAsync<T>(requestUri, requestBody, Timeout.InfiniteTimeSpan, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a PUT request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<T>> PutAsync<T>(
        string requestUri, string requestBody, TimeSpan requestTimeout,
        string contentType = MediaTypeNames.Text.Plain)
        where T : class
    {
        return await PutAsync<T>(new Uri(requestUri), requestBody, requestTimeout, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a PUT request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<T>> PutAsync<T>(
        Uri requestUri, string requestBody, TimeSpan requestTimeout,
        string contentType = MediaTypeNames.Text.Plain)
        where T : class
    {
        using var content = new StringContent(requestBody, Encoding.UTF8, contentType);
        return await SendRequestAsync<T>(HttpMethod.Put, requestUri, content, requestTimeout, true).ConfigureAwait(false);
    }
    #endregion

    #region [ Delete Requests ]
    /// <summary>
    /// Sends a DELETE request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> DeleteAsync(
        string requestUri, string requestBody,
        string contentType = MediaTypeNames.Text.Plain)
    {
        return await DeleteAsync(new Uri(requestUri), requestBody, Timeout.InfiniteTimeSpan, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a DELETE request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> DeleteAsync(
        Uri requestUri, string requestBody,
        string contentType = MediaTypeNames.Text.Plain)
    {
        return await DeleteAsync(requestUri, requestBody, Timeout.InfiniteTimeSpan, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a DELETE request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> DeleteAsync(
        string requestUri, string requestBody, TimeSpan requestTimeout,
        string contentType = MediaTypeNames.Text.Plain)
    {
        return await DeleteAsync(new Uri(requestUri), requestBody, requestTimeout, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a DELETE request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> DeleteAsync(
        Uri requestUri, string requestBody, TimeSpan requestTimeout,
        string contentType = MediaTypeNames.Text.Plain)
    {
        using var content = new StringContent(requestBody, Encoding.UTF8, contentType);
        return await SendRequestAsync<object>(HttpMethod.Delete, requestUri, content, requestTimeout, false).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a DELETE request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<T>> DeleteAsync<T>(
        Uri requestUri, string requestBody,
        string contentType = MediaTypeNames.Text.Plain)
        where T : class
    {
        return await DeleteAsync<T>(requestUri, requestBody, Timeout.InfiniteTimeSpan, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a DELETE request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<T>> DeleteAsync<T>(
        string requestUri, string requestBody, TimeSpan requestTimeout,
        string contentType = MediaTypeNames.Text.Plain)
        where T : class
    {
        return await DeleteAsync<T>(new Uri(requestUri), requestBody, requestTimeout, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a DELETE request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<T>> DeleteAsync<T>(
        Uri requestUri, string requestBody, TimeSpan requestTimeout,
        string contentType = MediaTypeNames.Text.Plain)
        where T : class
    {
        using var content = new StringContent(requestBody, Encoding.UTF8, contentType);
        return await SendRequestAsync<T>(HttpMethod.Delete, requestUri, content, requestTimeout, true).ConfigureAwait(false);
    }
    #endregion

    #region [ Patch Requests ]
    /// <summary>
    /// Sends a PATCH request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> PatchAsync(
        string requestUri, string requestBody,
        string contentType = MediaTypeNames.Text.Plain)
    {
        return await PatchAsync(new Uri(requestUri), requestBody, Timeout.InfiniteTimeSpan, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a PATCH request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> PatchAsync(
        Uri requestUri, string requestBody,
        string contentType = MediaTypeNames.Text.Plain)
    {
        return await PatchAsync(requestUri, requestBody, Timeout.InfiniteTimeSpan, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a PATCH request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> PatchAsync(
        string requestUri, string requestBody, TimeSpan requestTimeout,
        string contentType = MediaTypeNames.Text.Plain)
    {
        return await PatchAsync(new Uri(requestUri), requestBody, requestTimeout, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a PATCH request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<object>> PatchAsync(
        Uri requestUri, string requestBody, TimeSpan requestTimeout,
        string contentType = MediaTypeNames.Text.Plain)
    {
        using var content = new StringContent(requestBody, Encoding.UTF8, contentType);
        return await SendRequestAsync<object>(HttpMethod.Delete, requestUri, content, requestTimeout, false).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a PATCH request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<T>> PatchAsync<T>(
        Uri requestUri, string requestBody,
        string contentType = MediaTypeNames.Text.Plain)
        where T : class
    {
        return await PatchAsync<T>(requestUri, requestBody, Timeout.InfiniteTimeSpan, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a PATCH request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<T>> PatchAsync<T>(
        string requestUri, string requestBody, TimeSpan requestTimeout,
        string contentType = MediaTypeNames.Text.Plain)
        where T : class
    {
        return await PatchAsync<T>(new Uri(requestUri), requestBody, requestTimeout, contentType).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a PATCH request to the server.
    /// </summary>
    /// <param name="requestUri">Request URL</param>
    /// <param name="requestBody">Request message body/content</param>
    /// <param name="contentType">Specifies the request Content-Type</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    public async Task<WebResponse<T>> PatchAsync<T>(
        Uri requestUri, string requestBody, TimeSpan requestTimeout,
        string contentType = MediaTypeNames.Text.Plain)
        where T : class
    {
        using var content = new StringContent(requestBody, Encoding.UTF8, contentType);
        return await SendRequestAsync<T>(new HttpMethod("PATCH"), requestUri, content, requestTimeout, true).ConfigureAwait(false);
    }
    #endregion

    /// <summary>
    /// Sends the HTTP(S) request to the server.
    /// </summary>
    /// <param name="method">HTTP(S) verb/method to use</param>
    /// <param name="requestUri">Request URL</param>
    /// <param name="content">Request HTTP(S) content</param>
    /// <param name="requestTimeout">Request timeout</param>
    /// <param name="serialize">Determines whether to serialize the data or not</param>
    /// <typeparam name="T">Type used for deserialization of the response message body</typeparam>
    /// <returns>Web response object</returns>
    private async Task<WebResponse<T>> SendRequestAsync<T>(HttpMethod method, Uri requestUri, HttpContent? content, TimeSpan requestTimeout, bool serialize)
        where T : class
    {
        // Create request and add properties to the dictionary for authentication and the serialization pipeline
        // to be used with the request and response.
        using var request = new HttpTimeoutRequestMessage(method, requestUri, requestTimeout);
        request.Options.Set(new HttpRequestOptionsKey<AuthenticationPipeline?>(nameof(AuthenticationPipeline)), _authenticationPipeline);
        request.Options.Set(new HttpRequestOptionsKey<SerializerPipeline?>(nameof(SerializerPipeline)), _serializerPipeline);

        if (content is not null)
            request.Content = content;

        using HttpResponseMessage response = await _httpClient.SendAsync(request, _cancellationTokenSource.Token).ConfigureAwait(false);
        var messageBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        T? data = null;

        if (serialize)
        {
            if (request.Options.TryGetValue(new HttpRequestOptionsKey<ISerializer>(nameof(ISerializer)), out var serializer))
                data = serializer.Deserialize<T>(messageBody);
        }

        return new WebResponse<T>
        {
            Version = response.Version,
            ResponseCode = response.StatusCode,
            ReasonPhrase = response.ReasonPhrase,
            ResponseHeaders = response.Headers,
            Content = messageBody,
            Data = data,
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _httpClientHandler.Dispose();
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }
}