using System;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Aqueduct.Abstractions.Builders;

/// <summary>
///     Builder contract for Aqueduct server registration.
/// </summary>
public interface IAqueductServerBuilder
{
    /// <summary>
    ///     Configures services for the builder.
    /// </summary>
    /// <param name="configure">The services configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IAqueductServerBuilder ConfigureServices(Action<IServiceCollection> configure);

    /// <summary>
    ///     Configures Aqueduct options.
    /// </summary>
    /// <param name="configure">The options configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IAqueductServerBuilder ConfigureOptions(Action<AqueductOptions> configure);

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
}
