using System;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Client.BuiltIn.Lifecycle;
using Mississippi.Reservoir.Client.BuiltIn.Navigation;


namespace Mississippi.Reservoir.Client.BuiltIn;

/// <summary>
///     Extension methods for registering all built-in Reservoir Blazor features.
/// </summary>
/// <remarks>
///     <para>
///         This provides a convenient way to register all built-in features at once.
///         You can also register features individually using their specific extension methods.
///     </para>
///     <para>
///         Register with <see cref="AddReservoirBlazorBuiltIns(IReservoirBuilder)" /> after
///         calling <c>AddReservoir</c>.
///     </para>
/// </remarks>
public static class ReservoirBlazorBuiltInRegistrations
{
    /// <summary>
    ///     Adds all built-in Reservoir Blazor features to the Reservoir builder.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    public static IReservoirBuilder AddReservoirBlazorBuiltIns(
        this IReservoirBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddBuiltInNavigation();
        builder.AddBuiltInLifecycle();
        return builder;
    }
}