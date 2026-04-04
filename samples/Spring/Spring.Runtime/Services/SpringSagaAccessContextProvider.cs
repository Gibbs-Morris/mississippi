using Mississippi.DomainModeling.Abstractions;

using Orleans.Runtime;


namespace MississippiSamples.Spring.Runtime.Services;

/// <summary>
///     Reads the propagated saga access fingerprint from Orleans request context inside the runtime host.
/// </summary>
internal sealed class SpringSagaAccessContextProvider : ISagaAccessContextProvider
{
    /// <summary>
    ///     Request-context key used to flow the caller fingerprint from the gateway into Orleans grain calls.
    /// </summary>
    internal const string RequestContextKey = "spring.saga.access-fingerprint";

    /// <summary>
    ///     Gets the current caller fingerprint from Orleans request context when one is available.
    /// </summary>
    /// <returns>The propagated caller fingerprint, or <see langword="null" /> when none is present.</returns>
    public string? GetFingerprint() => RequestContext.Get(RequestContextKey) as string;
}