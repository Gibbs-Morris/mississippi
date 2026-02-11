using System;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;


namespace Mississippi.Aqueduct.Abstractions.Builders;

/// <summary>
///     Builder contract for Aqueduct server registration.
/// </summary>
public interface IAqueductServerBuilder : IMississippiBuilder<IAqueductServerBuilder>
{
    /// <summary>
    ///     Adds the Aqueduct backplane for the specified hub type.
    /// </summary>
    /// <typeparam name="THub">The hub type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IAqueductServerBuilder AddBackplane<THub>()
        where THub : Hub;

    /// <summary>
    ///     Adds the Aqueduct notifier for Orleans grains.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    IAqueductServerBuilder AddNotifier();

    /// <summary>
    ///     Configures Aqueduct options.
    /// </summary>
    /// <param name="configure">The options configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IAqueductServerBuilder ConfigureAqueductOptions(
        Action<AqueductOptions> configure
    );
}