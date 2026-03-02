using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Runtime.Abstractions;


namespace Mississippi.Common.Builders.Runtime;

/// <summary>
///     Runtime aggregate sub-builder implementation.
/// </summary>
/// <typeparam name="TSnapshot">Snapshot type.</typeparam>
public sealed class AggregateBuilder<TSnapshot> : IAggregateBuilder<TSnapshot>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateBuilder{TSnapshot}" /> class.
    /// </summary>
    /// <param name="services">Service collection.</param>
    public AggregateBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    private IServiceCollection Services { get; }

    /// <inheritdoc />
    public IAggregateBuilder<TSnapshot> AddSnapshotStateConverter<TConverter>()
        where TConverter : class
    {
        Services.AddTransient<TConverter>();
        return this;
    }
}