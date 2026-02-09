using System;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Common.Abstractions.Builders;

/// <summary>
///     Common builder contract for Mississippi registration.
/// </summary>
/// <typeparam name="TBuilder">The builder type.</typeparam>
public interface IMississippiBuilder<out TBuilder>
    where TBuilder : IMississippiBuilder<TBuilder>
{
    /// <summary>
    ///     Configures services for the builder.
    /// </summary>
    /// <param name="configure">The services configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    TBuilder ConfigureServices(Action<IServiceCollection> configure);

    /// <summary>
    ///     Configures options for the builder.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="configure">The options configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    TBuilder ConfigureOptions<TOptions>(Action<TOptions> configure)
        where TOptions : class;
}
