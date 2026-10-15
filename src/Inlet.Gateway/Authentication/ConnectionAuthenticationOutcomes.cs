using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Authentication;


namespace Mississippi.Inlet.Gateway.Authentication;

/// <summary>
///     Retains the final result returned for an explicitly requested authentication scheme.
/// </summary>
internal sealed class ConnectionAuthenticationOutcomes
{
    private Dictionary<string, AuthenticateResult> Results { get; } = new(StringComparer.Ordinal);

    /// <summary>
    ///     Finds an actual service outcome without running the handler or transformation again.
    /// </summary>
    /// <param name="scheme">The exact requested scheme.</param>
    /// <returns>The recorded result, when present.</returns>
    internal AuthenticateResult? GetResult(
        string scheme
    ) =>
        Results.GetValueOrDefault(scheme);

    /// <summary>
    ///     Records the result after the host authentication service has completed.
    /// </summary>
    /// <param name="scheme">The exact requested scheme.</param>
    /// <param name="result">The final, potentially transformed result.</param>
    internal void Record(
        string scheme,
        AuthenticateResult result
    ) =>
        Results[scheme] = ConnectionAuthenticationResults.CopyResult(result);
}