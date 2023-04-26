namespace RestUtility.Handlers;

internal sealed class TimeoutHandler : DelegatingHandler
{
    // Set a default timeout to match the HttpClient.Timeout for each of the request chains
    // if no explicit timeout has been set.
    public TimeSpan DefaultTimeout { get; } = TimeSpan.FromSeconds(100);

    private CancellationTokenSource? GetCancellationTokenSource(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Retrieve the timeout TimeSpan
        TimeSpan timeout = request is HttpTimeoutRequestMessage httpTimeoutRequestMessage
            ? httpTimeoutRequestMessage.RequestTimeout
            : DefaultTimeout;

        if (timeout == Timeout.InfiniteTimeSpan)
            return null;

        // Only set a cancellation token source if the timeout is not infinite.
        // (Linking the cancellation token with the timeout to the original one sent with the request.)
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        return cts;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            using CancellationTokenSource? cts = GetCancellationTokenSource(request, cancellationToken);
            return await base.SendAsync(request, cts?.Token ?? cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) // Exception filter avoids stack unwind
            when (ex.InnerException is OperationCanceledException // When exception thrown but cancellation not requested, this means a timeout occured
                  && !cancellationToken.IsCancellationRequested)
        {
            // Throw a TimeoutException only when cancellation isn't explicitly requested
            // on the main cancellation token for the request.
            throw new TimeoutException();
        }
    }
}