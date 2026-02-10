using System;

using Mississippi.Reservoir.Abstractions.Builders;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation;


namespace Mississippi.Reservoir.Blazor.BuiltIn;

/// <summary>
///     Extension methods for registering all built-in Reservoir Blazor features.
/// </summary>
/// <remarks>
///     <para>
///         This provides a convenient way to register all built-in features at once.
///         You can also register features individually using their specific extension methods.
///     </para>
///     <para>
///         Register with <see cref="AddReservoirBlazorBuiltIns" /> after calling <c>AddReservoir</c> on the builder.
///     </para>
/// </remarks>
public static class ReservoirBlazorBuiltInRegistrations
{
    /// <summary>
    ///     Adds all built-in Reservoir Blazor features to the Reservoir builder.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    /// <remarks>
    ///     This registers:
    ///     <list type="bullet">
    ///         <item>Navigation feature (state, reducers, effects)</item>
    ///         <item>Lifecycle feature (state, reducers)</item>
    ///     </list>
    ///     <para>
    ///         <strong>Important:</strong> You must also render the
    ///         <see cref="Components.ReservoirNavigationProvider" /> component in your app
    ///         to receive location change notifications.
    ///     </para>
    /// </remarks>
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