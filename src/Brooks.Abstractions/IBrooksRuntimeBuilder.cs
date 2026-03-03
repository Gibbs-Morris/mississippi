using System;

using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Common.Builders.Abstractions;


namespace Mississippi.Brooks.Abstractions;

/// <summary>
///     Contract for configuring Brooks runtime services through an <c>IRuntimeBuilder</c> extension surface.
/// </summary>
public interface IBrooksRuntimeBuilder : IMississippiBuilder
{
    /// <summary>
    ///     Configures Orleans stream provider options for Brooks runtime integration.
    /// </summary>
    /// <param name="configure">Options configuration delegate.</param>
    /// <returns>The same builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure" /> is null.</exception>
    IBrooksRuntimeBuilder ConfigureStreaming(
        Action<BrookProviderOptions> configure
    );
}