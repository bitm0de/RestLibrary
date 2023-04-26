using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;

// ReSharper disable StringLiteralTypo

namespace RestUtility.Authentication;

/// <inheritdoc />
/// <summary>
/// Digest authentication handler
/// </summary>
public sealed class DigestAuthenticationHandler : IRestClientAuthentication
{
    private int _nCount = 1;

    public string? Username { get; set; }
    public string? Password { get; set; }

    public bool CheckIfAuthenticationMatches(HttpResponseMessage response)
    {
        if (response is null)
            throw new ArgumentNullException(nameof(response));

        return response.Headers.WwwAuthenticate.Any(
            authenticationHeaderValue => authenticationHeaderValue.Scheme.Equals("Digest", StringComparison.OrdinalIgnoreCase));
    }

    public async Task ConfigureRequestAsync(HttpRequestMessage request, HttpResponseMessage previousResponse)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        Uri? requestUri = request.RequestUri ?? throw new InvalidOperationException("Request URI cannot be null.");
        HttpContent? requestContent = request.Content ?? throw new InvalidOperationException("Request content cannot be null.");

        if (previousResponse is null)
            throw new ArgumentNullException(nameof(previousResponse));

        foreach (AuthenticationHeaderValue? authenticationHeaderValue in previousResponse.Headers.WwwAuthenticate)
        {
            if (!authenticationHeaderValue.Scheme.Equals("Digest", StringComparison.OrdinalIgnoreCase))
                continue;

            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parts = authenticationHeaderValue.Parameter?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            foreach (var part in parts)
            {
                var kv = part.Split(new[] { '=' }, 2);
                if (kv.Length == 2)
                {
                    dictionary[kv[0].Trim()] = kv[1].Trim('"').Trim();
                }
            }

            string? ha1 = null;
            if (dictionary.TryGetValue("algorithm", out var algorithm))
            {
                if (algorithm.Equals("MD5", StringComparison.OrdinalIgnoreCase))
                {
                    ha1 = HashUtility.GetHashBytes<MD5>(Username + ":" + dictionary["realm"] + ":" + Password).ToLowerHex();
                }
                else if (algorithm.Equals("MD5-sess", StringComparison.OrdinalIgnoreCase))
                {
                    ha1 = HashUtility.GetHashBytes<MD5>(
                        HashUtility.GetHashBytes<MD5>(Username + ":" + dictionary["realm"] + ":" + Password).ToLowerHex()
                        + ":" + dictionary["nonce"] + ":" + dictionary["cnonce"]).ToLowerHex();
                }
                else if (algorithm.Equals("SHA-256", StringComparison.OrdinalIgnoreCase))
                {
                    ha1 = HashUtility.GetHashBytes<SHA256>(Username + ":" + dictionary["realm"] + ":" + Password).ToLowerHex();
                }
                else if (algorithm.Equals("SHA-256-sess", StringComparison.OrdinalIgnoreCase))
                {
                    ha1 = HashUtility.GetHashBytes<SHA256>(
                        HashUtility.GetHashBytes<SHA256>(Username + ":" + dictionary["realm"] + ":" + Password).ToLowerHex()
                        + ":" + dictionary["nonce"] + ":" + dictionary["cnonce"]).ToLowerHex();
                }
                else if (algorithm.Equals("SHA-512", StringComparison.OrdinalIgnoreCase))
                {
                    ha1 = HashUtility.GetHashBytes<SHA256>(Username + ":" + dictionary["realm"] + ":" + Password).ToLowerHex();
                }
                else if (algorithm.Equals("SHA-512-sess", StringComparison.OrdinalIgnoreCase))
                {
                    ha1 = HashUtility.GetHashBytes<SHA256>(
                        HashUtility.GetHashBytes<SHA256>(Username + ":" + dictionary["realm"] + ":" + Password).ToLowerHex()
                        + ":" + dictionary["nonce"] + ":" + dictionary["cnonce"]).ToLowerHex();
                }
            }
            else
            {
                ha1 = HashUtility.GetHashBytes<MD5>(Username + ":" + dictionary["realm"] + ":" + Password).ToLowerHex();
            }

            var digestParams = new List<string> {
                $"username=\"{Username}\"",
                $"realm=\"{dictionary["realm"]}\"",
                $"nonce=\"{dictionary["nonce"]}\"",
                $"uri=\"{request.RequestUri?.AbsolutePath}\"",
            };

            if (dictionary.TryGetValue("opaque", out var opaque))
                digestParams.Add($"opaque=\"{opaque}\"");

            string? ha2 = null;
            if (dictionary.TryGetValue("qop", out var qop))
            {
                // ReSharper disable once IdentifierTypo
                var cnonce = GetClientNonce();
                var qopDirectives = qop.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();

                if (Array.IndexOf(qopDirectives, "auth") != -1)
                    ha2 = HashUtility.GetHashBytes<MD5>(request.Method.Method + ":" + requestUri.AbsolutePath).ToLowerHex();
                else if (Array.IndexOf(qopDirectives, "auth-int") != -1)
                {
                    ha2 = HashUtility.GetHashBytes<MD5>(
                        request.Method.Method + ":" + requestUri.AbsolutePath + ":" +
                        HashUtility.GetHashBytes<MD5>(await requestContent.ReadAsStringAsync().ConfigureAwait(false)).ToLowerHex()
                    ).ToLowerHex();
                }

                digestParams.Add($"qop=\"{qop}\"");
                digestParams.Add($"nc=\"{_nCount:00000000}\"");
                digestParams.Add($"cnonce=\"{cnonce}\"");

                var response = HashUtility.GetHashBytes<MD5>(ha1 + ":" + dictionary["nonce"] + ":" + _nCount.ToString("D8", CultureInfo.InvariantCulture) + ":" + cnonce + ":" + qop + ":" + ha2).ToLowerHex();
                digestParams.Add($"response=\"{response}\"");
                request.Headers.Authorization = new AuthenticationHeaderValue("Digest", string.Join(", ", digestParams));
            }
            else
            {
                ha2 = HashUtility.GetHashBytes<MD5>(request.Method.Method + ":" + requestUri.AbsolutePath).ToLowerHex();
                var response = HashUtility.GetHashBytes<MD5>(ha1 + ":" + dictionary["nonce"] + ":" + ha2).ToLowerHex();
                digestParams.Add($"response=\"{response}\"");
                request.Headers.Authorization = new AuthenticationHeaderValue("Digest", string.Join(", ", digestParams));
            }

            _nCount++;
        }
    }

    private static string GetClientNonce() => Guid.NewGuid().ToString().Substring(0, 8);
}