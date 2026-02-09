using System;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.Reservoir.Abstractions.Builders;
using Mississippi.Reservoir.Builders;


namespace Mississippi.Reservoir;

/// <summary>
///     Extension methods for Reservoir builder registration.
/// </summary>
public static class ReservoirBuilderExtensions
{
    /// <summary>
    ///     Adds Reservoir services to the Mississippi client builder.
    /// </summary>
    /// <param name="builder">The Mississippi client builder.</param>
    /// <returns>The Reservoir builder.</returns>
    public static IReservoirBuilder AddReservoir(
        this IMississippiClientBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new ReservoirBuilder(builder.Services);
    }
}
