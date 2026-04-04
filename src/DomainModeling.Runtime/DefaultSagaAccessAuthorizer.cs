using System;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Provides the default fingerprint-based saga resource authorizer.
/// </summary>
/// <remarks>
///     The default behavior is intentionally conservative when a stored fingerprint exists: callers must present the
///     same fingerprint to read or resume the saga. Legacy or unscoped sagas without a stored fingerprint remain
///     accessible until a host provides richer authorization semantics.
/// </remarks>
internal sealed class DefaultSagaAccessAuthorizer : ISagaAccessAuthorizer
{
    /// <inheritdoc />
    public SagaAccessAuthorizationResult Authorize(
        string entityId,
        SagaAccessAction action,
        string? storedAccessContextFingerprint,
        string? currentAccessContextFingerprint
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        _ = action;
        if (string.IsNullOrWhiteSpace(storedAccessContextFingerprint))
        {
            return SagaAccessAuthorizationResult.Allow();
        }

        return string.Equals(storedAccessContextFingerprint, currentAccessContextFingerprint, StringComparison.Ordinal)
            ? SagaAccessAuthorizationResult.Allow()
            : SagaAccessAuthorizationResult.Deny("The current caller is not authorized for this saga.");
    }
}