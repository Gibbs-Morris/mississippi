using System;

using Mississippi.Common.Builders.Abstractions;


namespace Mississippi.Common.Builders.Client.Abstractions;

/// <summary>
///     Contract for configuring Mississippi client host composition.
/// </summary>
public interface IClientBuilder : IMississippiBuilder
{
    /// <summary>
    ///     Configures client-host builder options.
    /// </summary>
    /// <param name="configure">Options configuration delegate.</param>
    /// <returns>The same builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure" /> is null.</exception>
    IClientBuilder ConfigureClient(
        Action<ClientBuilderOptions> configure
    );
}