namespace RestUtility;

/// <inheritdoc />
/// <summary>
/// An extension to the <see cref="System.Net.Http.HttpResponseMessage" /> allowing for a timeout to be specified per request.
/// </summary>
internal sealed class HttpTimeoutRequestMessage : HttpRequestMessage
{
    /// <summary>
    /// Request timeout
    /// </summary>
    public TimeSpan RequestTimeout { get; }

    public HttpTimeoutRequestMessage(HttpMethod method, string requestUri, TimeSpan requestTimeout)
        : this(method, new Uri(requestUri), requestTimeout)
    { }

    public HttpTimeoutRequestMessage(HttpMethod method, Uri requestUri, TimeSpan requestTimeout)
        : base(method, requestUri)
    {
        RequestTimeout = requestTimeout;
    }
}