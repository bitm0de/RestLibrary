using System.Net;
using System.Net.Http.Headers;

namespace RestUtility;

/// <summary>
/// Web response object
/// </summary>
/// <typeparam name="T">Serialization type for the <see cref="Content"/> string</typeparam>
public sealed class WebResponse<T>
    where T : notnull
{
    public Version? Version { get; set; }
    public HttpStatusCode ResponseCode { get; set; }
    public string? ReasonPhrase { get; set; }
    public HttpResponseHeaders? ResponseHeaders { get; set; }
    public string? Content { get; set; }
    public T? Data { get; set; }

    public override string? ToString() => Content;
}