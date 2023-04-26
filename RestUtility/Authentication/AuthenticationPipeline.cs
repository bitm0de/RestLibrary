using System.Collections;

namespace RestUtility.Authentication;

/// <inheritdoc />
/// <summary>
/// Authentication pipeline for automatic data handling
/// </summary>
public sealed class AuthenticationPipeline : IEnumerable<IRestClientAuthentication>
{
    private readonly List<IRestClientAuthentication> _authentications;

    public AuthenticationPipeline(params IRestClientAuthentication[] authentications)
        : this((IEnumerable<IRestClientAuthentication>)authentications)
    { }

    public AuthenticationPipeline(IEnumerable<IRestClientAuthentication> authentications)
    {
        _authentications = new List<IRestClientAuthentication>(authentications);
    }

    public IEnumerator<IRestClientAuthentication> GetEnumerator()
        => _authentications.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}