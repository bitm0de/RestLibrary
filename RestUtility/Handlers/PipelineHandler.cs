using System.Net;
using RestUtility.Authentication;
using RestUtility.Serializers;

// .NET 6 - HttpRequestMessage.Metadata has been deprecated. Use Options instead.
#pragma warning disable CS0618

namespace RestUtility.Handlers;

/// <inheritdoc />
/// <summary>
/// Custom delegating handler for authentication and serialization/deserialization functionality
/// </summary>
internal sealed class PipelineHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // Check for '401 Unauthorized' response to configure authentication if necessary.
        if (response.StatusCode == HttpStatusCode.Unauthorized
            && request.Options.TryGetValue(new HttpRequestOptionsKey<AuthenticationPipeline>(nameof(AuthenticationPipeline)), out var authenticationPipeline))
        {
            foreach (IRestClientAuthentication? authentication in authenticationPipeline.Where(authentication => authentication.CheckIfAuthenticationMatches(response)))
            {
                await authentication.ConfigureRequestAsync(request, response).ConfigureAwait(false);
                response.Dispose();
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                break;
            }
        }

        // Check the Content-Type header to match a serializer with the MIME type from the pipeline
        if (response.Content.Headers.TryGetValues("Content-Type", out var contentTypeValues)
            && request.Options.TryGetValue(new HttpRequestOptionsKey<SerializerPipeline>(nameof(SerializerPipeline)), out var serializerPipeline))
        {
            foreach (var contentType in contentTypeValues)
            {
                if (serializerPipeline.Serializers.TryGetValue(contentType, out ISerializer? serializer))
                {
                    // Set the ISerializer to the key of the same name in the dictionary for the
                    // HttpClient to use for deserializing the data object of the specified type
                    request.Options.Set(new HttpRequestOptionsKey<ISerializer>(nameof(ISerializer)), serializer);
                    break;
                }
            }
        }

        return response;
    }
}