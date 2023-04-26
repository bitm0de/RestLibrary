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
        throw new NotImplementedException();
    }

    public Task ConfigureRequestAsync(HttpRequestMessage request, HttpResponseMessage previousResponse)
    {
        // TODO: Configure request to implement OAuth.
        throw new NotImplementedException();
    }
}