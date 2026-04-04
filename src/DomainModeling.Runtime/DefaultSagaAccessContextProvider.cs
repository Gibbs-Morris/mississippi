using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Provides the default no-fingerprint saga access context.
/// </summary>
/// <remarks>
///     Hosts should replace this implementation when they need resource-level saga authorization tied to caller identity.
/// </remarks>
internal sealed class DefaultSagaAccessContextProvider : ISagaAccessContextProvider
{
    /// <inheritdoc />
    public string? GetFingerprint() => null;
}