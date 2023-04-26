namespace RestUtility.Authentication;

/// <inheritdoc />
/// <summary>
/// OAuth authentication handler
/// </summary>
public sealed class OAuthAuthenticationHandler : IRestClientAuthentication
{
    public bool CheckIfAuthenticationMatches(HttpResponseMessage response)
    {
        // TODO: Check for OAuth authentication.
        return false;
    }

    public async Task ConfigureRequestAsync(HttpRequestMessage request, HttpResponseMessage previousResponse)
    {
        // TODO: Configure request to implement OAuth.
        await Task.FromResult<object?>(null);
    }
}