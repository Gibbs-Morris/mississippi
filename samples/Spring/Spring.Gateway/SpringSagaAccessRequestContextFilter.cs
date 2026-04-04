using System;
using System.Threading.Tasks;

using Orleans;
using Orleans.Runtime;


namespace MississippiSamples.Spring.Gateway;

/// <summary>
///     Copies the current gateway caller fingerprint into Orleans request context for outgoing grain calls.
/// </summary>
internal sealed class SpringSagaAccessRequestContextFilter : IOutgoingGrainCallFilter
{
    private ISagaAccessContextProvider AccessContextProvider { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SpringSagaAccessRequestContextFilter" /> class.
    /// </summary>
    /// <param name="accessContextProvider">Provider for the current caller fingerprint.</param>
    public SpringSagaAccessRequestContextFilter(
        ISagaAccessContextProvider accessContextProvider
    )
    {
        ArgumentNullException.ThrowIfNull(accessContextProvider);
        AccessContextProvider = accessContextProvider;
    }

    /// <summary>
    ///     Invokes the next outgoing grain call filter while scoping the caller fingerprint into request context.
    /// </summary>
    /// <param name="context">The outgoing grain call context.</param>
    /// <returns>A task that completes when the outgoing grain call has finished.</returns>
    public async Task Invoke(
        IOutgoingGrainCallContext context
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        object? previousValue = RequestContext.Get(SpringSagaAccessContextProvider.RequestContextKey);
        try
        {
            string? fingerprint = AccessContextProvider.GetFingerprint();
            if (string.IsNullOrWhiteSpace(fingerprint))
            {
                RequestContext.Remove(SpringSagaAccessContextProvider.RequestContextKey);
            }
            else
            {
                RequestContext.Set(SpringSagaAccessContextProvider.RequestContextKey, fingerprint);
            }

            await context.Invoke();
        }
        finally
        {
            if (previousValue is null)
            {
                RequestContext.Remove(SpringSagaAccessContextProvider.RequestContextKey);
            }
            else
            {
                RequestContext.Set(SpringSagaAccessContextProvider.RequestContextKey, previousValue);
            }
        }
    }
}