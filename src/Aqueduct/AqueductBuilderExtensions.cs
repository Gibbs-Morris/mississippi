using System;

using Microsoft.AspNetCore.Builder;

using Mississippi.Aqueduct.Abstractions.Builders;
using Mississippi.Aqueduct.Builders;
using Mississippi.Common.Abstractions.Builders;


namespace Mississippi.Aqueduct;

/// <summary>
///     Extension methods for Aqueduct builder registration.
/// </summary>
public static class AqueductBuilderExtensions
{
    /// <summary>
    ///     Creates an Aqueduct server builder for Mississippi server registration.
    /// </summary>
    /// <param name="builder">The Mississippi server builder.</param>
    /// <returns>The Aqueduct server builder.</returns>
    public static IAqueductServerBuilder AddAqueduct(
        this IMississippiServerBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new AqueductServerBuilder(builder);
    }

    /// <summary>
    ///     Creates an Aqueduct server builder for a web application.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The Aqueduct server builder.</returns>
    public static IAqueductServerBuilder AddAqueduct(
        this WebApplicationBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new AqueductServerBuilder(builder.Services);
    }
}