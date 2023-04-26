namespace RestUtility.Authentication;

/// <summary>
/// Authentication interface
/// </summary>
public interface IRestClientAuthentication
{
    bool CheckIfAuthenticationMatches(HttpResponseMessage response);
    Task ConfigureRequestAsync(HttpRequestMessage request, HttpResponseMessage previousResponse);
}