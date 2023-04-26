using System.Net.Http.Headers;

namespace RestUtility.Authentication;

/// <inheritdoc />
/// <summary>
/// Bearer authentication handler
/// </summary>
public sealed class BearerAuthenticationHandler : IRestClientAuthentication
{
    /// <summary>
    /// Bearer token
    /// </summary>
    public string? Token { get; set; }

    public bool CheckIfAuthenticationMatches(HttpResponseMessage response)
    {
        if (response is null)
            throw new ArgumentNullException(nameof(response));

        return response.Headers.WwwAuthenticate.Any(
            authenticationHeaderValue => authenticationHeaderValue.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase));
    }

    public async Task ConfigureRequestAsync(HttpRequestMessage request, HttpResponseMessage previousResponse)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        await Task.FromResult<object?>(null);
    }
}