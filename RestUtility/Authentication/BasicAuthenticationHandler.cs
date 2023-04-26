using System.Net.Http.Headers;
using System.Text;

namespace RestUtility.Authentication;

/// <inheritdoc />
/// <summary>
/// Basic authentication handler
/// </summary>
public sealed class BasicAuthenticationHandler : IRestClientAuthentication
{
    public string? Username { get; set; }
    public string? Password { get; set; }

    public bool CheckIfAuthenticationMatches(HttpResponseMessage response)
    {
        if (response is null)
            throw new ArgumentNullException(nameof(response));

        return response.Headers.WwwAuthenticate.Any(
            authenticationHeaderValue => authenticationHeaderValue.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase));
    }

    public async Task ConfigureRequestAsync(HttpRequestMessage request, HttpResponseMessage previousResponse)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", EncodeCredentials());
        await Task.FromResult<object?>(null);
    }

    private string EncodeCredentials()
    {
        var bytes = Encoding.UTF8.GetBytes(Username + ":" + Password);
        return Convert.ToBase64String(bytes);
    }
}