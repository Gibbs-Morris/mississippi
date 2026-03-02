using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Runtime.Abstractions;

using Orleans.Hosting;


namespace Mississippi.Common.Builders.Runtime;

/// <summary>
///     Concrete runtime-host builder implementation.
/// </summary>
public sealed class RuntimeBuilder : IRuntimeBuilder
{
    private RuntimeBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <summary>
    ///     Creates a new runtime builder instance.
    /// </summary>
    /// <returns>A new <see cref="RuntimeBuilder" /> instance.</returns>
    public static RuntimeBuilder Create() => new(new ServiceCollection());

    /// <inheritdoc />
    public IRuntimeBuilder ApplyToSilo(
        ISiloBuilder siloBuilder
    )
    {
        ArgumentNullException.ThrowIfNull(siloBuilder);
        return this;
    }

    /// <inheritdoc />
    public IRuntimeBuilder ConfigureSnapshotRetention(
        Action<SnapshotRetentionOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        Services.Configure(configure);
        return this;
    }
}