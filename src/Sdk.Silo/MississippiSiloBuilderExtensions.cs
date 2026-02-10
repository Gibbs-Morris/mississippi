using System;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.Sdk.Silo.Builders;

using Orleans.Hosting;


namespace Mississippi.Sdk.Silo;

/// <summary>
///     Extension methods for Mississippi silo builder registration.
/// </summary>
public static class MississippiSiloBuilderExtensions
{
    /// <summary>
    ///     Creates a Mississippi silo builder for an Orleans silo.
    /// </summary>
    /// <param name="builder">The Orleans silo builder.</param>
    /// <returns>The Mississippi silo builder.</returns>
    public static IMississippiSiloBuilder AddMississippiSilo(
        this ISiloBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new MississippiSiloBuilder(builder);
    }
}