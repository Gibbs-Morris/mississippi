using System.Collections.Generic;
using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Hosting.Abstractions;

/// <summary>
///     Defines the shared composition and validation contract for Mississippi root builders.
/// </summary>
/// <remarks>Public so role-specific builders and consumer extensions share a lightweight contract.</remarks>
public interface IMississippiBuilder
{
    /// <summary>
    ///     Gets the staged services for advanced composition extensions.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    IServiceCollection Services { get; }

    /// <summary>
    ///     Checks whether this composition is ready to attach without changing its registrations.
    /// </summary>
    /// <returns>The failures preventing attachment, or an empty list when ready.</returns>
    IReadOnlyList<BuilderDiagnostic> Validate();
}