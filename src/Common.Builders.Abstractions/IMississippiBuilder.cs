using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Common.Builders.Abstractions;

/// <summary>
///     Shared root contract for all Mississippi host-surface builders.
/// </summary>
public interface IMississippiBuilder
{
    /// <summary>
    ///     Gets the service collection used for immediate registration delegation.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    ///     Validates builder configuration and returns diagnostics.
    /// </summary>
    /// <returns>Validation diagnostics, or an empty list when valid.</returns>
    IReadOnlyList<BuilderDiagnostic> Validate();
}